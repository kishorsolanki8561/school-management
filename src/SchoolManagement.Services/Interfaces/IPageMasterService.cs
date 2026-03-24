using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IPageMasterService
{
    Task<PageResponse>              CreatePageAsync  (CreatePageRequest request,                    CancellationToken ct = default);
    Task<PageResponse>              UpdatePageAsync  (int id, UpdatePageRequest request,             CancellationToken ct = default);
    Task                            DeletePageAsync  (int id,                                        CancellationToken ct = default);
    Task<PageResponse?>             GetPageByIdAsync (int id,                                        CancellationToken ct = default);
    Task<PagedResult<PageResponse>> GetAllPagesAsync (PaginationRequest request, int? menuId = null, CancellationToken ct = default);
}
