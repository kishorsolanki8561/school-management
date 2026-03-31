using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class NotificationConfigServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext   _context;
    private readonly NotificationConfigService   _sut;

    public NotificationConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _sut     = new NotificationConfigService(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NewEmailConfig_PersistsAndReturns()
    {
        var request = EmailRequest("smtp.gmail.com", 587, "user@test.com");

        var result = await _sut.SaveAsync(orgId: 1, request);

        result.OrgId.Should().Be(1);
        result.Channel.Should().Be(NotificationChannel.Email);
        result.SmtpHost.Should().Be("smtp.gmail.com");
        result.SmtpPort.Should().Be(587);
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveAsync_ExistingConfig_UpdatesExisting()
    {
        await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.old.com", 25, "old@test.com"));

        var result = await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.new.com", 587, "new@test.com"));

        result.SmtpHost.Should().Be("smtp.new.com");
        var count = await _context.OrgNotificationConfigs.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_SoftDeletedConfig_RestoresOnUpsert()
    {
        await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.test.com", 587, "a@test.com"));
        await _sut.DeleteAsync(orgId: 1, NotificationChannel.Email);

        var result = await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.test.com", 587, "a@test.com"));

        result.IsActive.Should().BeTrue();
        var count = await _context.OrgNotificationConfigs.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_NewSmsConfig_PersistsWithSmsFields()
    {
        var request = SmsRequest(SmsProvider.Infobip, "MY_API_KEY", "MySchool");

        var result = await _sut.SaveAsync(orgId: 2, request);

        result.Channel.Should().Be(NotificationChannel.SMS);
        result.SmsProvider.Should().Be(SmsProvider.Infobip);
        result.SenderName.Should().Be("MySchool");
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_Exists_ReturnsConfig()
    {
        await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.test.com", 587, "a@test.com"));

        var result = await _sut.GetAsync(orgId: 1, NotificationChannel.Email);

        result.Should().NotBeNull();
        result!.SmtpHost.Should().Be("smtp.test.com");
    }

    [Fact]
    public async Task GetAsync_NotConfigured_ReturnsNull()
    {
        var result = await _sut.GetAsync(orgId: 999, NotificationChannel.Email);
        result.Should().BeNull();
    }

    // ── GetAllByOrgAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllByOrgAsync_MultipleChannels_ReturnsAll()
    {
        await _sut.SaveAsync(orgId: 3, EmailRequest("smtp.test.com", 587, "a@test.com"));
        await _sut.SaveAsync(orgId: 3, SmsRequest(SmsProvider.Twilio, "SID:TOKEN", "15005550006"));

        var result = await _sut.GetAllByOrgAsync(orgId: 3);

        result.Should().HaveCount(2);
        result.Select(r => r.Channel).Should().Contain(NotificationChannel.Email);
        result.Select(r => r.Channel).Should().Contain(NotificationChannel.SMS);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Exists_SoftDeletes()
    {
        await _sut.SaveAsync(orgId: 1, EmailRequest("smtp.test.com", 587, "a@test.com"));

        await _sut.DeleteAsync(orgId: 1, NotificationChannel.Email);

        var result = await _sut.GetAsync(orgId: 1, NotificationChannel.Email);
        result.Should().BeNull();

        var raw = await _context.OrgNotificationConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrgId == 1);
        raw!.IsDeleted.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SaveOrgNotificationConfigRequest EmailRequest(string host, int port, string from) =>
        new(NotificationChannel.Email,
            SmtpHost: host, SmtpPort: port, SmtpUsername: "user", SmtpPassword: "pass",
            FromAddress: from, FromName: "Test", EnableSsl: true,
            SmsProvider: null, ApiKey: null, AccountSid: null, AuthToken: null,
            SenderNumber: null, SenderName: null,
            PushServerKey: null, PushSenderId: null);

    private static SaveOrgNotificationConfigRequest SmsRequest(SmsProvider provider, string apiKey, string sender) =>
        new(NotificationChannel.SMS,
            SmtpHost: null, SmtpPort: null, SmtpUsername: null, SmtpPassword: null,
            FromAddress: null, FromName: null, EnableSsl: null,
            SmsProvider: provider, ApiKey: apiKey, AccountSid: null, AuthToken: null,
            SenderNumber: null, SenderName: sender,
            PushServerKey: null, PushSenderId: null);
}
