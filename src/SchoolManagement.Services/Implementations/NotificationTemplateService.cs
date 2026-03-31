using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly SchoolManagementDbContext _db;
    private readonly IRequestContext           _requestContext;

    public NotificationTemplateService(SchoolManagementDbContext db, IRequestContext requestContext)
    {
        _db             = db;
        _requestContext = requestContext;
    }

    private bool IsOwnerAdmin => _requestContext.Role == nameof(UserRole.OwnerAdmin);

    public async Task<NotificationTemplateResponse> SaveAsync(SaveNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var orgId = IsOwnerAdmin ? (int?)null : _requestContext.OrgId;

        var existing = await _db.NotificationTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrgId == orgId
                                   && x.EventType == request.EventType
                                   && x.Channel   == request.Channel, ct);

        if (existing is null)
        {
            existing = new NotificationTemplate
            {
                OrgId     = orgId,
                EventType = request.EventType,
                Channel   = request.Channel,
            };
            _db.NotificationTemplates.Add(existing);
        }
        else if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
        }

        existing.Subject     = request.Subject;
        existing.Body        = request.Body;
        existing.IsBodyHtml  = request.IsBodyHtml;
        existing.ToAddresses = request.ToAddresses;
        existing.CcAddresses = request.CcAddresses;
        existing.BccAddresses = request.BccAddresses;
        existing.IsActive    = true;

        await _db.SaveChangesAsync(ct);
        return ToResponse(existing);
    }

    public async Task<NotificationTemplateResponse?> GetAsync(
        int? orgId, NotificationEventType eventType, NotificationChannel? channel, CancellationToken ct = default)
    {
        var entity = await _db.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId     == orgId
                                   && x.EventType  == eventType
                                   && x.Channel    == channel
                                   && x.IsActive, ct);

        return entity is null ? null : ToResponse(entity);
    }

    public async Task<IReadOnlyList<NotificationTemplateResponse>> GetAllAsync(int? orgId, CancellationToken ct = default)
    {
        var list = await _db.NotificationTemplates
            .AsNoTracking()
            .Where(x => x.OrgId == orgId)
            .OrderBy(x => x.EventType).ThenBy(x => x.Channel)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.NotificationTemplates.FindAsync(new object[] { id }, ct);
        if (entity is null) return;
        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    private static NotificationTemplateResponse ToResponse(NotificationTemplate e) => new(
        e.Id, e.OrgId, e.EventType, e.Channel,
        e.Subject, e.Body, e.IsBodyHtml,
        e.ToAddresses, e.CcAddresses, e.BccAddresses, e.IsActive
    );
}
