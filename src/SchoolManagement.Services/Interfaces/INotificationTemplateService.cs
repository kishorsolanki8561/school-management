using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Services.Interfaces;

public interface INotificationTemplateService
{
    /// <summary>
    /// Upserts a template.
    /// OwnerAdmin → OrgId = null (global default).
    /// Org user   → OrgId = IRequestContext.OrgId.
    /// </summary>
    Task<NotificationTemplateResponse>              SaveAsync(SaveNotificationTemplateRequest request, CancellationToken ct = default);
    Task<NotificationTemplateResponse?>             GetAsync(int? orgId, NotificationEventType eventType, NotificationChannel? channel, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationTemplateResponse>> GetAllAsync(int? orgId, CancellationToken ct = default);
    Task                                            DeleteAsync(int id, CancellationToken ct = default);
}
