using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Services;
using SchoolManagement.Common.Utilities;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class UserManagementService : IUserManagementService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;
    private readonly IRequestContext          _requestContext;

    public UserManagementService(SchoolManagementDbContext context, IReadRepository readRepo, IRequestContext requestContext)
    {
        _context        = context;
        _readRepo       = readRepo;
        _requestContext = requestContext;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsOwnerAdmin => _requestContext.Role == nameof(UserRole.OwnerAdmin);
    private int? CallerOrgId  => IsOwnerAdmin ? null : _requestContext.OrgId;

    private async Task<UserResponse> BuildResponseAsync(int userId, CancellationToken ct)
    {
        var user = await _readRepo.QueryFirstOrDefaultAsync<UserResponse>(
                       UserManagementQueries.GetById, new { Id = userId })
                   ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(userId));

        var roles = (await _readRepo.QueryAsync<UserRoleResponse>(
                        UserManagementQueries.GetRolesByUserId,
                        new { UserId = userId, OrgId = CallerOrgId }))
                    .ToList();

        return new UserResponse
        {
            Id        = user.Id,
            Username  = user.Username,
            Email     = user.Email,
            IsActive  = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles     = roles,
        };
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var exists = await _context.Users
            .AnyAsync(u => (u.Username == request.Username || u.Email == request.Email) && !u.IsDeleted, ct);

        if (exists)
            throw new InvalidOperationException(AppMessages.UserManagement.UsernameTaken);

        var orgId = CallerOrgId
                    ?? throw new InvalidOperationException("OrgId is required to create a user.");

        var user = new User
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = HashingUtility.HashPassword(request.Password),
        };

        // Validate and attach roles
        var distinctRoleIds = request.RoleIds?.Where(r => r != 0).Distinct().ToList() ?? new List<int>();
        foreach (var roleId in distinctRoleIds)
        {
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId && !r.IsDeleted, ct);
            if (!roleExists)
                throw new KeyNotFoundException(AppMessages.UserManagement.RoleNotFound(roleId));
        }

        var roleMappings = distinctRoleIds
            .Select(roleId => new UserRoleMapping { User = user, RoleId = roleId, OrgId = orgId })
            .ToList();

        var orgMapping = new UserOrganizationMapping { User = user, OrgId = orgId };

        await _context.Users.AddAsync(user, ct);
        _context.UserRoleMappings.AddRange(roleMappings);
        await _context.UserOrganizationMappings.AddAsync(orgMapping, ct);
        await _context.SaveChangesAsync(ct);

        return await BuildResponseAsync(user.Id, ct);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, ct)
                   ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(id));

        user.Username = request.Username;
        user.Email    = request.Email;
        user.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);
        return await BuildResponseAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, ct)
                   ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(id));

        user.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public Task<UserResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => BuildResponseAsync(id, ct)
           .ContinueWith(t => t.IsCompletedSuccessfully ? (UserResponse?)t.Result : null);

    public async Task<PagedResult<UserResponse>> GetAllAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var param = new
        {
            OrgId    = CallerOrgId,
            Search   = request.Search,
            IsActive = request.Status,
            DateFrom = request.DateFrom,
            DateTo   = request.DateTo,
            Offset   = request.Offset,
            request.PageSize,
        };

        var dataSql = QueryBuilder.AppendPaging(
            UserManagementQueries.GetAll,
            request.SortBy, request.SortDescending,
            UserManagementQueries.AllowedSortColumns,
            UserManagementQueries.DefaultSortColumn);

        var paged = await _readRepo.QueryPagedAsync<UserResponse>(
            dataSql, UserManagementQueries.CountAll, param, request.Page, request.PageSize);

        // Enrich each user with their roles
        var orgIdParam = CallerOrgId;
        foreach (var user in paged.Items)
        {
            var roles = (await _readRepo.QueryAsync<UserRoleResponse>(
                            UserManagementQueries.GetRolesByUserId,
                            new { UserId = user.Id, OrgId = orgIdParam }))
                        .ToList();
            ((List<UserRoleResponse>)user.Roles).AddRange(roles);
        }

        return paged;
    }

    // ── Role Assignment ───────────────────────────────────────────────────────

    public async Task<UserResponse> AssignRoleAsync(int userId, AssignRoleRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, ct)
                   ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(userId));

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId && !r.IsDeleted, ct);
        if (!roleExists)
            throw new KeyNotFoundException(AppMessages.UserManagement.RoleNotFound(request.RoleId));

        var orgId = CallerOrgId;

        var alreadyAssigned = await _context.UserRoleMappings
            .AnyAsync(m => m.UserId == userId && m.RoleId == request.RoleId
                        && m.OrgId == orgId && !m.IsDeleted, ct);

        if (alreadyAssigned)
            throw new InvalidOperationException(AppMessages.UserManagement.RoleAlreadyAssigned(request.RoleId));

        var mapping = new UserRoleMapping { UserId = userId, RoleId = request.RoleId, OrgId = orgId };
        await _context.UserRoleMappings.AddAsync(mapping, ct);
        await _context.SaveChangesAsync(ct);

        return await BuildResponseAsync(userId, ct);
    }

    public async Task<UserResponse> RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default)
    {
        _ = await _context.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(userId));

        var orgId = CallerOrgId;

        var mapping = await _context.UserRoleMappings
            .FirstOrDefaultAsync(m => m.UserId == userId && m.RoleId == roleId
                                   && m.OrgId == orgId && !m.IsDeleted, ct)
            ?? throw new KeyNotFoundException(AppMessages.UserManagement.RoleAssignmentNotFound(userId, roleId));

        mapping.IsDeleted = true;
        await _context.SaveChangesAsync(ct);

        return await BuildResponseAsync(userId, ct);
    }

    // ── Role Level Change (OwnerAdmin only) ───────────────────────────────────

    public async Task<UserResponse> ChangeRoleLevelAsync(int userId, ChangeRoleLevelRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(request.TargetRole, ignoreCase: true, out var targetRole)
            || (targetRole != UserRole.SuperAdmin && targetRole != UserRole.Admin))
            throw new InvalidOperationException(AppMessages.UserManagement.InvalidRoleLevel);

        var user = await _context.Users
            .Include(u => u.UserRoleMappings.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new KeyNotFoundException(AppMessages.UserManagement.NotFound(userId));

        // Guard: cannot modify OwnerAdmin account
        var ownerAdminRoleId = (int)UserRole.OwnerAdmin;
        if (user.UserRoleMappings.Any(m => m.RoleId == ownerAdminRoleId))
            throw new InvalidOperationException(AppMessages.UserManagement.CannotModifyOwnerAdmin);

        // Remove existing SuperAdmin / Admin system role mappings (OrgId IS NULL)
        var systemRoleIds = new[] { (int)UserRole.SuperAdmin, (int)UserRole.Admin };
        var existingSystemRoles = user.UserRoleMappings
            .Where(m => systemRoleIds.Contains(m.RoleId) && m.OrgId == null)
            .ToList();

        foreach (var m in existingSystemRoles)
            m.IsDeleted = true;

        // Assign target role (system-level, no OrgId)
        var targetRoleId = (int)targetRole;
        var newMapping = new UserRoleMapping { UserId = userId, RoleId = targetRoleId, OrgId = null };
        await _context.UserRoleMappings.AddAsync(newMapping, ct);
        await _context.SaveChangesAsync(ct);

        return await BuildResponseAsync(userId, ct);
    }
}
