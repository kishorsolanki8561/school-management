using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IMenuAndPagePermissionService
{
    Task<MenuAndPagePermissionResponse>              UpdateAsync   (int id, int roleId,                                                                   CancellationToken ct = default);
    Task<MenuAndPagePermissionResponse?>             GetByIdAsync  (int id,                                                                            CancellationToken ct = default);
    Task<PagedResult<MenuAndPagePermissionResponse>> GetAllAsync   (PaginationRequest request, int? menuId = null, int? pageId = null, int? roleId = null, CancellationToken ct = default);
}
