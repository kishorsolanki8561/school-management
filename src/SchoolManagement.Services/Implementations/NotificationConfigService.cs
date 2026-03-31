using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class NotificationConfigService : INotificationConfigService
{
    private readonly SchoolManagementDbContext _db;

    public NotificationConfigService(SchoolManagementDbContext db)
    {
        _db = db;
    }

    public async Task<OrgNotificationConfigResponse> SaveAsync(int orgId, SaveOrgNotificationConfigRequest request, CancellationToken ct = default)
    {
        var existing = await _db.OrgNotificationConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrgId == orgId && x.Channel == request.Channel, ct);

        if (existing is null)
        {
            existing = new OrgNotificationConfig { OrgId = orgId, Channel = request.Channel };
            _db.OrgNotificationConfigs.Add(existing);
        }
        else if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
        }

        // Email
        existing.SmtpHost     = request.SmtpHost;
        existing.SmtpPort     = request.SmtpPort;
        existing.SmtpUsername = request.SmtpUsername;
        existing.SmtpPassword = request.SmtpPassword;
        existing.FromAddress  = request.FromAddress;
        existing.FromName     = request.FromName;
        existing.EnableSsl    = request.EnableSsl;

        // SMS
        existing.SmsProvider  = request.SmsProvider;
        existing.ApiKey       = request.ApiKey;
        existing.AccountSid   = request.AccountSid;
        existing.AuthToken    = request.AuthToken;
        existing.SenderNumber = request.SenderNumber;
        existing.SenderName   = request.SenderName;

        // Push
        existing.PushServerKey = request.PushServerKey;
        existing.PushSenderId  = request.PushSenderId;

        existing.IsActive = true;

        await _db.SaveChangesAsync(ct);
        return ToResponse(existing);
    }

    public async Task<OrgNotificationConfigResponse?> GetAsync(int orgId, NotificationChannel channel, CancellationToken ct = default)
    {
        var entity = await _db.OrgNotificationConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == orgId && x.Channel == channel, ct);

        return entity is null ? null : ToResponse(entity);
    }

    public async Task<IReadOnlyList<OrgNotificationConfigResponse>> GetAllByOrgAsync(int orgId, CancellationToken ct = default)
    {
        var list = await _db.OrgNotificationConfigs
            .AsNoTracking()
            .Where(x => x.OrgId == orgId)
            .OrderBy(x => x.Channel)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(int orgId, NotificationChannel channel, CancellationToken ct = default)
    {
        var entity = await _db.OrgNotificationConfigs
            .FirstOrDefaultAsync(x => x.OrgId == orgId && x.Channel == channel, ct);

        if (entity is null) return;
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    private static OrgNotificationConfigResponse ToResponse(OrgNotificationConfig e) => new(
        e.Id, e.OrgId, e.Channel, e.IsActive,
        e.SmtpHost, e.SmtpPort, e.SmtpUsername, e.FromAddress, e.FromName, e.EnableSsl,
        e.SmsProvider, e.SenderNumber, e.SenderName,
        e.PushSenderId
    );
}
