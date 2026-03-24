using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Constants;
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
            ParentMenuId           = request.ParentMenuId,
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
        menu.ParentMenuId           = request.ParentMenuId;
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

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Collect page IDs belonging to this menu (used for child table queries)
            var pageIds = await _context.PageMasters
                .Where(p => p.MenuId == id)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (pageIds.Count > 0)
            {
                // MenuAndPagePermissions — filter by MenuId (covers all pages of this menu)
                var permissions = await _context.MenuAndPagePermissions
                    .Where(p => p.MenuId == id)
                    .ToListAsync(cancellationToken);
                foreach (var p in permissions) p.IsDeleted = true;

                // PageMasterModuleActionMappings
                var actions = await _context.PageMasterModuleActionMappings
                    .Where(a => pageIds.Contains(a.PageId))
                    .ToListAsync(cancellationToken);
                foreach (var a in actions) a.IsDeleted = true;

                // PageMasterModules
                var modules = await _context.PageMasterModules
                    .Where(m => pageIds.Contains(m.PageId))
                    .ToListAsync(cancellationToken);
                foreach (var m in modules) m.IsDeleted = true;

                // PageMasters
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
    }

    public Task<MenuResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _readRepo.QueryFirstOrDefaultAsync<MenuResponse>(MenuMasterQueries.GetById, new { Id = id });

    public async Task<PagedResult<MenuResponse>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
        => await _readRepo.QueryPagedAsync<MenuResponse>(
            MenuMasterQueries.GetAll,
            MenuMasterQueries.CountAll,
            new { Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search, Offset = (request.Page - 1) * request.PageSize, request.PageSize },
            request.Page,
            request.PageSize);
}
