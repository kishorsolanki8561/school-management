using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IMenuAndPagePermissionService
{
    Task<MenuAndPagePermissionResponse>              UpdateAsync          (int id, int roleId,                                                                   CancellationToken ct = default);
    Task<MenuAndPagePermissionResponse?>             GetByIdAsync         (int id,                                                                            CancellationToken ct = default);
    Task<PagedResult<MenuAndPagePermissionResponse>> GetAllAsync          (PaginationRequest request, int? menuId = null, int? pageId = null, int? roleId = null, CancellationToken ct = default);

    /// <summary>Toggle IsAllowed on an org-specific permission row. SuperAdmin/Admin only.</summary>
    Task<MenuAndPagePermissionResponse>              UpdateOrgPermissionAsync(int id, int roleId, int orgId, CancellationToken ct = default);

    /// <summary>Get all org-specific permissions for a role within an org.</summary>
    Task<PagedResult<MenuAndPagePermissionResponse>> GetOrgPermissionsAsync  (int orgId, PaginationRequest request, int? menuId = null, int? pageId = null, int? roleId = null, CancellationToken ct = default);
}
