using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface ISchoolService
{
    Task<SchoolResponse>                       RegisterAsync(RegisterSchoolRequest request, CancellationToken ct = default);
    Task<SchoolResponse>                       UpdateAsync(int id, UpdateSchoolRequest request, CancellationToken ct = default);
    Task                                       DeleteAsync(int id, CancellationToken ct = default);
    Task<SchoolResponse?>                      GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<SchoolResponse>>          GetAllAsync(PaginationRequest request, bool? isApproved = null, CancellationToken ct = default);

    Task<SchoolResponse>                       ApproveAsync(int id, CancellationToken ct = default);
    Task<SchoolResponse>                       RejectAsync(int id, RejectSchoolRequest request, CancellationToken ct = default);
    Task<PagedResult<ApprovalRequestResponse>> GetPendingApprovalsAsync(PaginationRequest request, CancellationToken ct = default);
    Task<IList<ApprovalRequestResponse>>       GetApprovalHistoryAsync(int orgId, CancellationToken ct = default);
}
