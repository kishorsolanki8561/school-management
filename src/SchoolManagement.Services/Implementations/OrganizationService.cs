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

public sealed class OrganizationService : IOrganizationService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository _readRepo;
    private readonly IMapper _mapper;

    public OrganizationService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context = context;
        _readRepo = readRepo;
        _mapper   = mapper;
    }

    public async Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Organizations
            .AnyAsync(o => o.Name == request.Name, cancellationToken);

        if (exists)
            throw new InvalidOperationException(AppMessages.Organization.AlreadyExists(request.Name));

        var org = new Organization
        {
            Name    = request.Name,
            Address = request.Address,
        };

        await _context.Organizations.AddAsync(org, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrganizationResponse>(org);
    }

    public async Task<OrganizationResponse> UpdateAsync(int id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.Organization.NotFound(id));

        org.Name     = request.Name;
        org.Address  = request.Address;
        org.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<OrganizationResponse>(org);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.Organization.NotFound(id));

        org.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrganizationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _readRepo.QueryFirstOrDefaultAsync<OrganizationResponse>(
            OrganizationQueries.GetById,
            new { Id = id });
    }

    public async Task<PagedResult<OrganizationResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            Search   = pagination.Search,
            IsActive = pagination.Status,
            DateFrom = pagination.DateFrom,
            DateTo   = pagination.DateTo,
            pagination.PageSize,
            Offset   = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            OrganizationQueries.GetAll,
            pagination.SortBy, pagination.SortDescending,
            OrganizationQueries.AllowedSortColumns, OrganizationQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<OrganizationResponse>(
            dataSql,
            OrganizationQueries.CountAll,
            param,
            pagination.Page,
            pagination.PageSize);
    }
}
