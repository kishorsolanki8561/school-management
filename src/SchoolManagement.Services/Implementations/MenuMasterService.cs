using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class MenuMasterService : IMenuMasterService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;
    private readonly IMapper                  _mapper;

    public MenuMasterService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context  = context;
        _readRepo = readRepo;
        _mapper   = mapper;
    }

    public async Task<MenuResponse> CreateAsync(CreateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = new MenuMaster
        {
            Name                   = request.Name,
            HasChild               = request.HasChild,
            ParentMenuId           = request.ParentMenuId == 0 ? null : request.ParentMenuId,
            Position               = request.Position,
            IconClass              = request.IconClass,
            IsUseMenuForOwnerAdmin = request.IsUseMenuForOwnerAdmin,
        };

        await _context.MenuMasters.AddAsync(menu, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MenuResponse>(menu);
    }

    public async Task<MenuResponse> UpdateAsync(int id, UpdateMenuRequest request, CancellationToken cancellationToken = default)
    {
        var menu = await _context.MenuMasters.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.MenuMaster.NotFound(id));

        menu.Name                   = request.Name;
        menu.HasChild               = request.HasChild;
        menu.ParentMenuId           = request.ParentMenuId == 0 ? null : request.ParentMenuId;
        menu.Position               = request.Position;
        menu.IconClass              = request.IconClass;
        menu.IsActive               = request.IsActive;
        menu.IsUseMenuForOwnerAdmin = request.IsUseMenuForOwnerAdmin;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MenuResponse>(menu);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await _context.MenuMasters.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.MenuMaster.NotFound(id));

        await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var pageIds = await _context.PageMasters
                    .Where(p => p.MenuId == id)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                if (pageIds.Count > 0)
                {
                    var permissions = await _context.MenuAndPagePermissions
                        .Where(p => p.MenuId == id)
                        .ToListAsync(cancellationToken);
                    foreach (var p in permissions) p.IsDeleted = true;

                    var actions = await _context.PageMasterModuleActionMappings
                        .Where(a => pageIds.Contains(a.PageId))
                        .ToListAsync(cancellationToken);
                    foreach (var a in actions) a.IsDeleted = true;

                    var modules = await _context.PageMasterModules
                        .Where(m => pageIds.Contains(m.PageId))
                        .ToListAsync(cancellationToken);
                    foreach (var m in modules) m.IsDeleted = true;

                    var pages = await _context.PageMasters
                        .Where(p => p.MenuId == id)
                        .ToListAsync(cancellationToken);
                    foreach (var p in pages) p.IsDeleted = true;
                }

                menu.IsDeleted = true;
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public Task<MenuResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _readRepo.QueryFirstOrDefaultAsync<MenuResponse>(MenuMasterQueries.GetById, new { Id = id });

    public async Task<PagedResult<MenuResponse>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            Search   = request.Search,
            IsActive = request.Status,
            DateFrom = request.DateFrom,
            DateTo   = request.DateTo,
            Offset   = request.Offset,
            request.PageSize,
        };

        var dataSql = QueryBuilder.AppendPaging(
            MenuMasterQueries.GetAll,
            request.SortBy, request.SortDescending,
            MenuMasterQueries.AllowedSortColumns, MenuMasterQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<MenuResponse>(
            dataSql, MenuMasterQueries.CountAll, param, request.Page, request.PageSize);
    }

    /// <summary>
    /// Returns breadcrumb object for the given menu <paramref name="id"/>.
    /// Equivalent to the SQL function Fn_GetMenus(@id).
    /// FullPath example: "Settings, Users, Permissions"
    /// Nodes: ordered list of { Id, Name } from root down to the requested menu.
    /// </summary>
    public async Task<BreadcrumbResponse> GetBreadcrumbAsync(int id, CancellationToken cancellationToken = default)
    {
        var lookup = await BuildMenuLookupAsync(cancellationToken);

        if (!lookup.TryGetValue(id, out var target))
            throw new KeyNotFoundException(AppMessages.MenuMaster.NotFound(id));

        var nodes = BuildBreadcrumbNodes(lookup, id);

        return new BreadcrumbResponse
        {
            Id       = target.Id,
            Name     = target.Name,
            FullPath = string.Join(", ", nodes.Select(n => n.Name)),
            Nodes    = nodes,
        };
    }

    /// <summary>
    /// Returns all permission detail rows for a given <paramref name="roleId"/>.
    /// Ordered by MenuName → PageName.
    /// </summary>
    public async Task<IList<PermissionDetailResponse>> GetPermissionDetailsAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Load menu tree for breadcrumb resolution (single round-trip) ──
        var menuLookup = await BuildMenuLookupAsync(cancellationToken);

        // ── 2. Flat join filtered by RoleId: Permissions ⋈ Pages ⋈ Modules ─
        var rows = await (
            from mp in _context.MenuAndPagePermissions.AsNoTracking()
            join p  in _context.PageMasters.AsNoTracking()       on mp.PageId       equals p.Id
            join m  in _context.PageMasterModules.AsNoTracking() on mp.PageModuleId equals m.Id
            where mp.RoleId == roleId
            orderby mp.MenuId, p.Name
            select new
            {
                mp.Id,
                mp.MenuId,
                PageName       = p.Name,
                mp.PageModuleId,
                PageModuleName = m.Name,
                mp.ActionId,
                mp.IsAllowed,
                mp.RoleId,
            })
            .ToListAsync(cancellationToken);

        // ── 3. Map → response, resolving breadcrumb from in-memory lookup ───
        return rows.Select(r => new PermissionDetailResponse
        {
            Id             = r.Id,
            MenuName       = menuLookup.ContainsKey(r.MenuId)
                                 ? string.Join(", ", BuildBreadcrumbNodes(menuLookup, r.MenuId).Select(n => n.Name))
                                 : string.Empty,
            PageName       = r.PageName,
            PageModuleId   = r.PageModuleId,
            PageModuleName = r.PageModuleName,
            ActionId       = r.ActionId,
            IsAllowed      = r.IsAllowed,
            RoleId         = r.RoleId,
        }).ToList();
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>Loads all active, non-deleted menus into an Id-keyed lookup.</summary>
    private async Task<Dictionary<int, MenuNode>> BuildMenuLookupAsync(CancellationToken ct)
    {
        var menus = await _context.MenuMasters
            .Where(m => m.IsActive && !m.IsDeleted)
            .Select(m => new MenuNode(m.Id, m.Name, m.ParentMenuId))
            .ToListAsync(ct);

        return menus.ToDictionary(m => m.Id);
    }

    /// <summary>Walks up from <paramref name="menuId"/> to root, returns ordered nodes root → target.</summary>
    private static IList<BreadcrumbNodeResponse> BuildBreadcrumbNodes(Dictionary<int, MenuNode> lookup, int menuId)
    {
        var nodes   = new List<BreadcrumbNodeResponse>();
        var current = lookup.GetValueOrDefault(menuId);

        while (current is not null)
        {
            nodes.Add(new BreadcrumbNodeResponse { Id = current.Id, Name = current.Name });
            current = current.ParentMenuId.HasValue
                      && lookup.TryGetValue(current.ParentMenuId.Value, out var parent)
                          ? parent
                          : null;
        }

        nodes.Reverse();   // root → target
        return nodes;
    }

    private sealed record MenuNode(int Id, string Name, int? ParentMenuId);
}
