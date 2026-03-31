using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class SchoolService : ISchoolService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;
    private readonly IRequestContext          _requestContext;

    public SchoolService(SchoolManagementDbContext context, IReadRepository readRepo, IRequestContext requestContext)
    {
        _context        = context;
        _readRepo       = readRepo;
        _requestContext = requestContext;
    }

    public async Task<SchoolResponse> RegisterAsync(RegisterSchoolRequest request, CancellationToken ct = default)
    {
        var exists = await _context.Organizations
            .AnyAsync(o => o.Name == request.Name, ct);

        if (exists)
            throw new InvalidOperationException(AppMessages.School.AlreadyExists(request.Name));

        var userId = int.Parse(_requestContext.UserId ?? "0");

        var org = new Organization
        {
            Name    = request.Name,
            Address = request.Address,
        };

        await _context.Organizations.AddAsync(org, ct);
        await _context.SaveChangesAsync(ct);

        // Create approval request
        var approvalRequest = new SchoolApprovalRequest
        {
            OrgId             = org.Id,
            RequestedByUserId = userId,
            Status            = ApprovalStatus.Pending,
        };

        await _context.SchoolApprovalRequests.AddAsync(approvalRequest, ct);

        // Link the requesting user to the org
        var userOrgMapping = new UserOrganizationMapping
        {
            UserId = userId,
            OrgId  = org.Id,
        };

        await _context.UserOrganizationMappings.AddAsync(userOrgMapping, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(org.Id, ct)
               ?? throw new KeyNotFoundException(AppMessages.School.NotFound(org.Id));
    }

    public async Task<SchoolResponse> UpdateAsync(int id, UpdateSchoolRequest request, CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { id }, ct)
                  ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));

        org.Name     = request.Name;
        org.Address  = request.Address;
        org.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
               ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { id }, ct)
                  ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));

        org.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public Task<SchoolResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => _readRepo.QueryFirstOrDefaultAsync<SchoolResponse>(SchoolQueries.GetById, new { Id = id });

    public async Task<PagedResult<SchoolResponse>> GetAllAsync(
        PaginationRequest request, bool? isApproved = null, CancellationToken ct = default)
    {
        var param = new
        {
            Search     = request.Search,
            IsActive   = request.Status,
            IsApproved = isApproved.HasValue ? (isApproved.Value ? 1 : 0) : (int?)null,
            DateFrom   = request.DateFrom,
            DateTo     = request.DateTo,
            Offset     = request.Offset,
            request.PageSize,
        };

        var dataSql = QueryBuilder.AppendPaging(
            SchoolQueries.GetAll,
            request.SortBy, request.SortDescending,
            SchoolQueries.AllowedSortColumns, SchoolQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<SchoolResponse>(
            dataSql, SchoolQueries.CountAll, param, request.Page, request.PageSize);
    }

    public async Task<SchoolResponse> ApproveAsync(int id, CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { id }, ct)
                  ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));

        if (org.IsApproved)
            throw new InvalidOperationException(AppMessages.School.AlreadyApproved(id));

        // Find the latest pending approval request
        var approvalRequest = await _context.SchoolApprovalRequests
            .Where(r => r.OrgId == id && r.Status == ApprovalStatus.Pending && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(AppMessages.School.NoPendingRequest);

        org.IsApproved  = true;
        org.ApprovedAt  = DateTime.UtcNow;
        org.ApprovedBy  = _requestContext.Username;

        approvalRequest.Status           = ApprovalStatus.Approved;
        approvalRequest.ReviewedByUserId = int.TryParse(_requestContext.UserId, out var uid) ? uid : null;
        approvalRequest.ReviewedAt       = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Copy all system roles + their permissions for this org
        await CopySystemRolesAndPermissionsAsync(id, ct);

        return await GetByIdAsync(id, ct)
               ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));
    }

    public async Task<SchoolResponse> RejectAsync(int id, RejectSchoolRequest request, CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { id }, ct)
                  ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));

        var approvalRequest = await _context.SchoolApprovalRequests
            .Where(r => r.OrgId == id && r.Status == ApprovalStatus.Pending && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(AppMessages.School.NoPendingRequest);

        approvalRequest.Status           = ApprovalStatus.Rejected;
        approvalRequest.RejectionReason  = request.RejectionReason;
        approvalRequest.ReviewedByUserId = int.TryParse(_requestContext.UserId, out var uid) ? uid : null;
        approvalRequest.ReviewedAt       = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct)
               ?? throw new KeyNotFoundException(AppMessages.School.NotFound(id));
    }

    public async Task<PagedResult<ApprovalRequestResponse>> GetPendingApprovalsAsync(
        PaginationRequest request, CancellationToken ct = default)
    {
        var param = new { Offset = request.Offset, request.PageSize };
        return await _readRepo.QueryPagedAsync<ApprovalRequestResponse>(
            SchoolQueries.GetPendingApprovals, SchoolQueries.CountPendingApprovals,
            param, request.Page, request.PageSize);
    }

    public async Task<IList<ApprovalRequestResponse>> GetApprovalHistoryAsync(int orgId, CancellationToken ct = default)
    {
        var rows = await _readRepo.QueryAsync<ApprovalRequestResponse>(
            SchoolQueries.GetApprovalHistory, new { OrgId = orgId });
        return rows.ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// On school approval, copies every system role (OrgId IS NULL) and all their
    /// MenuAndPagePermissions into org-specific rows tagged with the new OrgId.
    /// Idempotent — skips roles/permissions already copied for this org.
    /// </summary>
    private async Task CopySystemRolesAndPermissionsAsync(int orgId, CancellationToken ct)
    {
        // Load all system roles (OrgId IS NULL) with their permissions
        var systemRoles = await _context.Roles
            .Where(r => r.OrgId == null && !r.IsDeleted)
            .Include(r => r.Permissions.Where(p => !p.IsDeleted))
            .ToListAsync(ct);

        // Load already-copied role IDs for this org to ensure idempotency
        var alreadyCopiedSystemIds = await _context.Roles
            .Where(r => r.OrgId == orgId && r.SystemRoleId != null && !r.IsDeleted)
            .Select(r => r.SystemRoleId!.Value)
            .ToListAsync(ct);

        foreach (var systemRole in systemRoles)
        {
            if (alreadyCopiedSystemIds.Contains(systemRole.Id))
                continue;

            var orgRole = new Role
            {
                Name         = systemRole.Name,
                Description  = systemRole.Description,
                IsOrgRole    = true,
                OrgId        = orgId,
                SystemRoleId = systemRole.Id,
            };

            await _context.Roles.AddAsync(orgRole, ct);
            await _context.SaveChangesAsync(ct); // flush to get orgRole.Id

            var orgPermissions = systemRole.Permissions.Select(p => new MenuAndPagePermission
            {
                MenuId       = p.MenuId,
                PageId       = p.PageId,
                PageModuleId = p.PageModuleId,
                ActionId     = p.ActionId,
                RoleId       = orgRole.Id,
                IsAllowed    = p.IsAllowed,
                OrgId        = orgId,
            }).ToList();

            if (orgPermissions.Count > 0)
            {
                await _context.MenuAndPagePermissions.AddRangeAsync(orgPermissions, ct);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}
