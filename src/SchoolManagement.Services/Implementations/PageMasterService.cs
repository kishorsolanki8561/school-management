using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class PageMasterService : IPageMasterService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;
    private readonly IMapper                  _mapper;

    public PageMasterService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context  = context;
        _readRepo = readRepo;
        _mapper   = mapper;
    }

    // ── Page ──────────────────────────────────────────────────────────────────

    public async Task<PageResponse> CreatePageAsync(CreatePageRequest request, CancellationToken ct = default)
    {
        var menu = await _context.MenuMasters.FindAsync(new object[] { request.MenuId }, ct)
            ?? throw new KeyNotFoundException(AppMessages.MenuMaster.NotFound(request.MenuId));

        if (!menu.HasChild)
        {
            var count = await _context.PageMasters
                .CountAsync(p => p.MenuId == request.MenuId, ct);

            if (count >= 1)
                throw new InvalidOperationException(AppMessages.PageMaster.SinglePageViolation(request.MenuId));
        }

        var page = _mapper.Map<PageMaster>(request);

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.PageMasters.AddAsync(page, ct);
            await _context.SaveChangesAsync(ct);

            if (request.Modules.Count > 0)
            {
                var roleIds = await _context.Roles.Select(r => r.Id).ToListAsync(ct);

                var existingActions = new HashSet<(int moduleId, ActionType action)>();
                var existingPerms   = new HashSet<(int moduleId, ActionType action, int roleId)>();

                foreach (var moduleInput in request.Modules)
                    await SeedModuleAsync(page, moduleInput, roleIds, existingActions, existingPerms, ct);

                await _context.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return _mapper.Map<PageResponse>(page);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PageResponse> UpdatePageAsync(int id, UpdatePageRequest request, CancellationToken ct = default)
    {
        var page = await _context.PageMasters.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException(AppMessages.PageMaster.NotFound(id));

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _mapper.Map(request, page);

            if (request.Modules is not null)
            {
                var roleIds = await _context.Roles.Select(r => r.Id).ToListAsync(ct);

                var existingActions = (await _context.PageMasterModuleActionMappings
                    .Where(m => m.PageId == id)
                    .Select(m => new { m.PageModuleId, m.ActionId })
                    .ToListAsync(ct))
                    .Select(x => (x.PageModuleId, x.ActionId))
                    .ToHashSet();

                var existingPerms = (await _context.MenuAndPagePermissions
                    .Where(p => p.PageId == id)
                    .Select(p => new { p.PageModuleId, p.ActionId, p.RoleId })
                    .ToListAsync(ct))
                    .Select(x => (x.PageModuleId, x.ActionId, x.RoleId))
                    .ToHashSet();

                foreach (var moduleInput in request.Modules)
                {
                    var module = await _context.PageMasterModules
                        .FirstOrDefaultAsync(m => m.PageId == page.Id && m.Name == moduleInput.Name, ct);

                    if (module is null)
                    {
                        module = new PageMasterModule { Name = moduleInput.Name, PageId = page.Id };
                        await _context.PageMasterModules.AddAsync(module, ct);
                        await _context.SaveChangesAsync(ct);
                    }

                    await SeedActionsForModule(page, module, moduleInput, roleIds, existingActions, existingPerms, ct);
                }
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return _mapper.Map<PageResponse>(page);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task DeletePageAsync(int id, CancellationToken ct = default)
    {
        var page = await _context.PageMasters.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException(AppMessages.PageMaster.NotFound(id));

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var permissions = await _context.MenuAndPagePermissions
                .Where(p => p.PageId == id).ToListAsync(ct);
            foreach (var p in permissions) p.IsDeleted = true;

            var actions = await _context.PageMasterModuleActionMappings
                .Where(a => a.PageId == id).ToListAsync(ct);
            foreach (var a in actions) a.IsDeleted = true;

            var modules = await _context.PageMasterModules
                .Where(m => m.PageId == id).ToListAsync(ct);
            foreach (var m in modules) m.IsDeleted = true;

            page.IsDeleted = true;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public Task<PageResponse?> GetPageByIdAsync(int id, CancellationToken ct = default)
        => _readRepo.QueryFirstOrDefaultAsync<PageResponse>(PageMasterQueries.GetPageById, new { Id = id });

    public async Task<PagedResult<PageResponse>> GetAllPagesAsync(PaginationRequest request, int? menuId = null, CancellationToken ct = default)
        => await _readRepo.QueryPagedAsync<PageResponse>(
            PageMasterQueries.GetAllPages,
            PageMasterQueries.CountAllPages,
            new { MenuId = menuId, Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search, Offset = (request.Page - 1) * request.PageSize, request.PageSize },
            request.Page,
            request.PageSize);

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task SeedModuleAsync(
        PageMaster                                             page,
        CreatePageModuleInput                                  moduleInput,
        IList<int>                                             roleIds,
        HashSet<(int moduleId, ActionType action)>             existingActions,
        HashSet<(int moduleId, ActionType action, int roleId)> existingPerms,
        CancellationToken                                      ct)
    {
        var module = new PageMasterModule { Name = moduleInput.Name, PageId = page.Id };
        await _context.PageMasterModules.AddAsync(module, ct);
        await _context.SaveChangesAsync(ct);

        await SeedActionsForModule(page, module, moduleInput, roleIds, existingActions, existingPerms, ct);
    }

    private async Task SeedActionsForModule(
        PageMaster                                             page,
        PageMasterModule                                       module,
        CreatePageModuleInput                                  moduleInput,
        IList<int>                                             roleIds,
        HashSet<(int moduleId, ActionType action)>             existingActions,
        HashSet<(int moduleId, ActionType action, int roleId)> existingPerms,
        CancellationToken                                      ct)
    {
        var actionTypes = (moduleInput.Actions is null || moduleInput.Actions.Count == 0)
            ? (IEnumerable<ActionType>)Enum.GetValues<ActionType>()
            : moduleInput.Actions;

        var newMappings = new List<PageMasterModuleActionMapping>();
        var newPerms    = new List<MenuAndPagePermission>();

        foreach (var actionType in actionTypes)
        {
            var actionKey = (module.Id, actionType);
            if (!existingActions.Contains(actionKey))
            {
                newMappings.Add(new PageMasterModuleActionMapping
                {
                    PageId       = page.Id,
                    PageModuleId = module.Id,
                    ActionId     = actionType,
                });
                existingActions.Add(actionKey);
            }

            foreach (var roleId in roleIds)
            {
                var permKey = (module.Id, actionType, roleId);
                if (!existingPerms.Contains(permKey))
                {
                    newPerms.Add(new MenuAndPagePermission
                    {
                        MenuId       = page.MenuId,
                        PageId       = page.Id,
                        PageModuleId = module.Id,
                        ActionId     = actionType,
                        RoleId       = roleId,
                        IsAllowed    = false,
                    });
                    existingPerms.Add(permKey);
                }
            }
        }

        if (newMappings.Count > 0)
            await _context.PageMasterModuleActionMappings.AddRangeAsync(newMappings, ct);

        if (newPerms.Count > 0)
            await _context.MenuAndPagePermissions.AddRangeAsync(newPerms, ct);
    }
}
