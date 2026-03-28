using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Common.Constants;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class StateService : IStateService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository _readRepo;
    private readonly IMapper _mapper;

    public StateService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context = context;
        _readRepo = readRepo;
        _mapper = mapper;
    }

    public async Task<StateResponse> CreateAsync(CreateStateRequest request, CancellationToken cancellationToken = default)
    {
        var countryExists = await _context.Countries
            .AnyAsync(c => c.Id == request.CountryId, cancellationToken);

        if (!countryExists)
            throw new KeyNotFoundException(AppMessages.Country.NotFound(request.CountryId));

        var exists = await _context.States
            .AnyAsync(s => s.Name == request.Name && s.CountryId == request.CountryId, cancellationToken);

        if (exists)
            throw new InvalidOperationException(AppMessages.State.AlreadyExists(request.Name));

        var state = new State
        {
            Name = request.Name,
            Code = request.Code,
            CountryId = request.CountryId,
        };

        await _context.States.AddAsync(state, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation property so AutoMapper can resolve CountryName
        await _context.Entry(state).Reference(s => s.Country).LoadAsync(cancellationToken);

        return _mapper.Map<StateResponse>(state);
    }

    public async Task<StateResponse> UpdateAsync(int id, UpdateStateRequest request, CancellationToken cancellationToken = default)
    {
        var state = await _context.States
            .Include(s => s.Country)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.State.NotFound(id));

        state.Name = request.Name;
        state.Code = request.Code;
        state.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<StateResponse>(state);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var state = await _context.States
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.State.NotFound(id));

        state.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StateResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _readRepo.QueryFirstOrDefaultAsync<StateResponse>(
            StateQueries.GetById,
            new { Id = id });
    }

    public async Task<PagedResult<StateResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
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
            StateQueries.GetAll,
            pagination.SortBy, pagination.SortDescending,
            StateQueries.AllowedSortColumns, StateQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<StateResponse>(
            dataSql, StateQueries.CountAll, param, pagination.Page, pagination.PageSize);
    }

    public async Task<PagedResult<StateResponse>> GetByCountryAsync(int countryId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            CountryId = countryId,
            Search    = pagination.Search,
            IsActive  = pagination.Status,
            DateFrom  = pagination.DateFrom,
            DateTo    = pagination.DateTo,
            pagination.PageSize,
            Offset    = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            StateQueries.GetByCountry,
            pagination.SortBy, pagination.SortDescending,
            StateQueries.AllowedSortColumns, StateQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<StateResponse>(
            dataSql, StateQueries.CountByCountry, param, pagination.Page, pagination.PageSize);
    }
}
