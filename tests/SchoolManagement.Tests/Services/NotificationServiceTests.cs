using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using SchoolManagement.Services.Interfaces;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly NotificationService       _sut;

    // Channel handler mocks
    private readonly Mock<INotificationChannelHandler> _emailHandlerMock = new();
    private readonly Mock<INotificationChannelHandler> _smsHandlerMock   = new();
    private readonly Mock<INotificationChannelHandler> _inAppHandlerMock = new();

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);

        _emailHandlerMock.Setup(h => h.Channel).Returns(NotificationChannel.Email);
        _smsHandlerMock  .Setup(h => h.Channel).Returns(NotificationChannel.SMS);
        _inAppHandlerMock.Setup(h => h.Channel).Returns(NotificationChannel.InApp);

        // Default: all handlers succeed
        SetupHandler(_emailHandlerMock, true);
        SetupHandler(_smsHandlerMock,   true);
        SetupHandler(_inAppHandlerMock, true);

        IEnumerable<INotificationChannelHandler> handlers = new INotificationChannelHandler[]
        {
            _emailHandlerMock.Object,
            _smsHandlerMock.Object,
            _inAppHandlerMock.Object
        };

        _sut = new NotificationService(_context, handlers);
    }

    public void Dispose() => _context.Dispose();

    // ── SendAsync — dispatches to all enabled channels ────────────────────────

    [Fact]
    public async Task SendAsync_AllEnabledChannels_DispatchesToEachChannel()
    {
        await SeedOrgConfig(orgId: 1, NotificationChannel.Email);
        await SeedOrgConfig(orgId: 1, NotificationChannel.SMS);

        var request = MakeRequest(orgId: 1);
        var result  = await _sut.SendAsync(request);

        result.Results.Should().HaveCount(3); // Email + SMS + InApp (always added)
        result.Results.Should().Contain(r => r.Channel == NotificationChannel.Email);
        result.Results.Should().Contain(r => r.Channel == NotificationChannel.SMS);
        result.Results.Should().Contain(r => r.Channel == NotificationChannel.InApp);
    }

    // ── SendAsync — caller-specified channels override org config ─────────────

    [Fact]
    public async Task SendAsync_CallerSpecifiedChannels_OnlyDispatchesThose()
    {
        // Org has Email + SMS configured, but caller only wants Email
        await SeedOrgConfig(orgId: 2, NotificationChannel.Email);
        await SeedOrgConfig(orgId: 2, NotificationChannel.SMS);

        var request = MakeRequest(orgId: 2, channels: new[] { NotificationChannel.Email });
        var result  = await _sut.SendAsync(request);

        result.Results.Should().HaveCount(1);
        result.Results.Single().Channel.Should().Be(NotificationChannel.Email);

        _smsHandlerMock.Verify(
            h => h.SendAsync(It.IsAny<int>(), It.IsAny<NotificationTemplate?>(),
                             It.IsAny<NotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SendAsync — InApp always included even with no config ─────────────────

    [Fact]
    public async Task SendAsync_NoOrgConfig_InAppStillDispatched()
    {
        // No OrgNotificationConfigs at all for org 3
        var request = MakeRequest(orgId: 3);
        var result  = await _sut.SendAsync(request);

        result.Results.Should().HaveCount(1);
        result.Results.Single().Channel.Should().Be(NotificationChannel.InApp);

        _inAppHandlerMock.Verify(
            h => h.SendAsync(It.IsAny<int>(), It.IsAny<NotificationTemplate?>(),
                             It.IsAny<NotificationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SendAsync — one channel failure does not block others ─────────────────

    [Fact]
    public async Task SendAsync_OneChannelFails_OtherChannelsStillSucceed()
    {
        await SeedOrgConfig(orgId: 4, NotificationChannel.Email);
        // SMS throws
        _smsHandlerMock.Setup(h => h.SendAsync(
                It.IsAny<int>(), It.IsAny<NotificationTemplate?>(),
                It.IsAny<NotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChannelResult(NotificationChannel.SMS, false, "SMTP connection refused"));

        var request = MakeRequest(orgId: 4,
            channels: new[] { NotificationChannel.Email, NotificationChannel.SMS });
        var result  = await _sut.SendAsync(request);

        result.Results.Should().HaveCount(2);
        result.Results.First(r => r.Channel == NotificationChannel.Email).Success.Should().BeTrue();
        result.Results.First(r => r.Channel == NotificationChannel.SMS).Success.Should().BeFalse();
    }

    // ── SendAsync — template resolution: org+channel > org+generic ────────────

    [Fact]
    public async Task SendAsync_OrgChannelTemplateExists_UsedOverOrgGeneric()
    {
        const int orgId = 5;
        await SeedTemplate(orgId, NotificationEventType.SchoolApproved,
            channel: null,                    subject: "Generic Subject");
        await SeedTemplate(orgId, NotificationEventType.SchoolApproved,
            channel: NotificationChannel.Email, subject: "Email-Specific Subject");

        NotificationTemplate? capturedTemplate = null;
        _emailHandlerMock.Setup(h => h.SendAsync(
                It.IsAny<int>(), It.IsAny<NotificationTemplate?>(),
                It.IsAny<NotificationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<int, NotificationTemplate?, NotificationRequest, CancellationToken>(
                (_, t, _, _) => capturedTemplate = t)
            .ReturnsAsync(new ChannelResult(NotificationChannel.Email, true, null));

        var request = MakeRequest(orgId: orgId, channels: new[] { NotificationChannel.Email });
        await _sut.SendAsync(request);

        capturedTemplate.Should().NotBeNull();
        capturedTemplate!.Subject.Should().Be("Email-Specific Subject");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task SeedOrgConfig(int orgId, NotificationChannel channel)
    {
        _context.OrgNotificationConfigs.Add(new()
        {
            OrgId   = orgId,
            Channel = channel,
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedTemplate(int? orgId, NotificationEventType eventType,
        NotificationChannel? channel, string subject)
    {
        _context.NotificationTemplates.Add(new()
        {
            OrgId     = orgId,
            EventType = eventType,
            Channel   = channel,
            Subject   = subject,
            Body      = "body",
            IsBodyHtml = false,
            IsActive  = true
        });
        await _context.SaveChangesAsync();
    }

    private static void SetupHandler(Mock<INotificationChannelHandler> mock, bool success)
    {
        mock.Setup(h => h.SendAsync(
                It.IsAny<int>(), It.IsAny<NotificationTemplate?>(),
                It.IsAny<NotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, NotificationTemplate? _, NotificationRequest _, CancellationToken _) =>
                new ChannelResult(mock.Object.Channel, success, success ? null : "error"));
    }

    private static NotificationRequest MakeRequest(
        int                    orgId,
        NotificationChannel[]? channels = null) =>
        new(orgId,
            EventType:    NotificationEventType.SchoolApproved,
            Placeholders: new Dictionary<string, string> { ["SchoolName"] = "Test School" },
            ToEmail:      "test@school.com",
            ToPhone:      null,
            ToUserId:     1,
            DeviceToken:  null,
            Channels:     channels);
}
