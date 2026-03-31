using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Services.Interfaces;

public interface INotificationConfigService
{
    Task<OrgNotificationConfigResponse>              SaveAsync(int orgId, SaveOrgNotificationConfigRequest request, CancellationToken ct = default);
    Task<OrgNotificationConfigResponse?>             GetAsync(int orgId, NotificationChannel channel, CancellationToken ct = default);
    Task<IReadOnlyList<OrgNotificationConfigResponse>> GetAllByOrgAsync(int orgId, CancellationToken ct = default);
    Task                                             DeleteAsync(int orgId, NotificationChannel channel, CancellationToken ct = default);
}
