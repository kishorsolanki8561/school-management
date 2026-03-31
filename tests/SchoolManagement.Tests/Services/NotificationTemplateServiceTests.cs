using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class NotificationTemplateServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext      _context;
    private readonly Mock<IRequestContext>          _requestCtxMock = new();
    private readonly NotificationTemplateService    _sut;

    public NotificationTemplateServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);

        // Default: regular org user with OrgId = 1
        _requestCtxMock.Setup(r => r.Role).Returns("Admin");
        _requestCtxMock.Setup(r => r.OrgId).Returns(1);

        _sut = new NotificationTemplateService(_context, _requestCtxMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── SaveAsync — OwnerAdmin creates global template ────────────────────────

    [Fact]
    public async Task SaveAsync_OwnerAdmin_CreatesGlobalTemplate()
    {
        _requestCtxMock.Setup(r => r.Role).Returns("OwnerAdmin");

        var request = MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email);
        var result  = await _sut.SaveAsync(request);

        result.OrgId.Should().BeNull();
        result.EventType.Should().Be(NotificationEventType.SchoolApproved);
        result.Channel.Should().Be(NotificationChannel.Email);
    }

    // ── SaveAsync — org user creates org-specific template ────────────────────

    [Fact]
    public async Task SaveAsync_OrgUser_CreatesOrgSpecificTemplate()
    {
        var request = MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email);
        var result  = await _sut.SaveAsync(request);

        result.OrgId.Should().Be(1);
        result.EventType.Should().Be(NotificationEventType.SchoolApproved);
    }

    // ── SaveAsync — upsert ────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_SameOrgEventChannel_UpdatesExisting()
    {
        var first = MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email,
            subject: "Old Subject");
        await _sut.SaveAsync(first);

        var second = MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email,
            subject: "New Subject");
        var result = await _sut.SaveAsync(second);

        result.Subject.Should().Be("New Subject");
        var count = await _context.NotificationTemplates.CountAsync();
        count.Should().Be(1);
    }

    // ── SaveAsync — generic template (Channel = null) ─────────────────────────

    [Fact]
    public async Task SaveAsync_NullChannel_CreatesGenericTemplate()
    {
        var request = MakeRequest(NotificationEventType.FeeReminder, channel: null);
        var result  = await _sut.SaveAsync(request);

        result.Channel.Should().BeNull();
    }

    // ── SaveAsync — restore soft-deleted ─────────────────────────────────────

    [Fact]
    public async Task SaveAsync_SoftDeletedTemplate_RestoresOnUpsert()
    {
        var request = MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email);
        var created = await _sut.SaveAsync(request);
        await _sut.DeleteAsync(created.Id);

        var result = await _sut.SaveAsync(request);

        result.IsActive.Should().BeTrue();
        var count = await _context.NotificationTemplates.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_Exists_ReturnsTemplate()
    {
        await _sut.SaveAsync(MakeRequest(NotificationEventType.SchoolRejected, NotificationChannel.SMS));

        var result = await _sut.GetAsync(orgId: 1, NotificationEventType.SchoolRejected, NotificationChannel.SMS);

        result.Should().NotBeNull();
        result!.EventType.Should().Be(NotificationEventType.SchoolRejected);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        var result = await _sut.GetAsync(orgId: 99, NotificationEventType.SchoolApproved, NotificationChannel.Email);
        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllTemplatesForOrg()
    {
        await _sut.SaveAsync(MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.Email));
        await _sut.SaveAsync(MakeRequest(NotificationEventType.SchoolRejected, NotificationChannel.Email));
        await _sut.SaveAsync(MakeRequest(NotificationEventType.SchoolApproved, NotificationChannel.SMS));

        var result = await _sut.GetAllAsync(orgId: 1);

        result.Should().HaveCount(3);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Exists_SoftDeletes()
    {
        var created = await _sut.SaveAsync(
            MakeRequest(NotificationEventType.UserRegistered, NotificationChannel.Email));

        await _sut.DeleteAsync(created.Id);

        var result = await _sut.GetAsync(orgId: 1, NotificationEventType.UserRegistered, NotificationChannel.Email);
        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SaveNotificationTemplateRequest MakeRequest(
        NotificationEventType eventType,
        NotificationChannel?  channel = NotificationChannel.Email,
        string subject = "Subject {{SchoolName}}",
        string body    = "<p>Hello {{AdminName}}</p>") =>
        new(eventType, channel, subject, body,
            IsBodyHtml: true,
            ToAddresses: null, CcAddresses: null, BccAddresses: null);
}
