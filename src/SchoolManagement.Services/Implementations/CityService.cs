using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Common.Constants;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class CityService : ICityService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository _readRepo;
    private readonly IMapper _mapper;

    public CityService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context = context;
        _readRepo = readRepo;
        _mapper = mapper;
    }

    public async Task<CityResponse> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        var stateExists = await _context.States
            .AnyAsync(s => s.Id == request.StateId, cancellationToken);

        if (!stateExists)
            throw new KeyNotFoundException(AppMessages.State.NotFound(request.StateId));

        var exists = await _context.Cities
            .AnyAsync(c => c.Name == request.Name && c.StateId == request.StateId, cancellationToken);

        if (exists)
            throw new InvalidOperationException(AppMessages.City.AlreadyExists(request.Name));

        var city = new City
        {
            Name = request.Name,
            StateId = request.StateId,
        };

        await _context.Cities.AddAsync(city, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties so AutoMapper can resolve StateName / CountryName
        await _context.Entry(city).Reference(c => c.State).LoadAsync(cancellationToken);
        await _context.Entry(city.State).Reference(s => s.Country).LoadAsync(cancellationToken);

        return _mapper.Map<CityResponse>(city);
    }

    public async Task<CityResponse> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default)
    {
        var city = await _context.Cities
            .Include(c => c.State).ThenInclude(s => s.Country)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.City.NotFound(id));

        city.Name = request.Name;
        city.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CityResponse>(city);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _context.Cities
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.City.NotFound(id));

        city.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CityResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _readRepo.QueryFirstOrDefaultAsync<CityResponse>(
            CityQueries.GetById,
            new { Id = id });
    }

    public async Task<PagedResult<CityResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            Search = pagination.Search,
            pagination.PageSize,
            Offset = pagination.Offset,
        };

        return await _readRepo.QueryPagedAsync<CityResponse>(
            CityQueries.GetAll,
            CityQueries.CountAll,
            param,
            pagination.Page,
            pagination.PageSize);
    }

    public async Task<PagedResult<CityResponse>> GetByStateAsync(int stateId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            StateId = stateId,
            pagination.PageSize,
            Offset = pagination.Offset,
        };

        return await _readRepo.QueryPagedAsync<CityResponse>(
            CityQueries.GetByState,
            CityQueries.CountByState,
            param,
            pagination.Page,
            pagination.PageSize);
    }
}
