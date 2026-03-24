using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class MenuAndPagePermissionService : IMenuAndPagePermissionService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;

    public MenuAndPagePermissionService(SchoolManagementDbContext context, IReadRepository readRepo)
    {
        _context  = context;
        _readRepo = readRepo;
    }

    public async Task<MenuAndPagePermissionResponse> UpdateAsync(int id, UpdatePermissionRequest request, CancellationToken ct = default)
    {
        var permission = await _context.MenuAndPagePermissions.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException(AppMessages.MenuAndPagePermission.NotFound(id));

        permission.IsAllowed = request.IsAllowed;
        await _context.SaveChangesAsync(ct);

        return new MenuAndPagePermissionResponse
        {
            Id           = permission.Id,
            MenuId       = permission.MenuId,
            PageId       = permission.PageId,
            PageModuleId = permission.PageModuleId,
            ActionId     = permission.ActionId,
            RoleId       = permission.RoleId,
            IsAllowed    = permission.IsAllowed,
            CreatedAt    = permission.CreatedAt,
        };
    }

    public Task<MenuAndPagePermissionResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => _readRepo.QueryFirstOrDefaultAsync<MenuAndPagePermissionResponse>(
               MenuAndPagePermissionQueries.GetById, new { Id = id });

    public async Task<PagedResult<MenuAndPagePermissionResponse>> GetAllAsync(
        PaginationRequest request,
        int? menuId = null,
        int? pageId = null,
        int? roleId = null,
        CancellationToken ct = default)
        => await _readRepo.QueryPagedAsync<MenuAndPagePermissionResponse>(
               MenuAndPagePermissionQueries.GetAll,
               MenuAndPagePermissionQueries.CountAll,
               new { MenuId = menuId, PageId = pageId, RoleId = roleId, Offset = (request.Page - 1) * request.PageSize, request.PageSize },
               request.Page,
               request.PageSize);
}
