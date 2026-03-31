using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Common;

namespace SchoolManagement.Services.Interfaces;

public interface IInAppNotificationService
{
    Task<PagedResult<InAppNotificationResponse>> GetForUserAsync(int userId, bool? unreadOnly, PaginationRequest request, CancellationToken ct = default);
    Task<int>                                    GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task                                         MarkReadAsync(int userId, MarkReadRequest request, CancellationToken ct = default);
    Task                                         MarkAllReadAsync(int userId, CancellationToken ct = default);
}
