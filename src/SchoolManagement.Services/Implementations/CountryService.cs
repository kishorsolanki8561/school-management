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

public sealed class CountryService : ICountryService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository _readRepo;
    private readonly IMapper _mapper;

    public CountryService(SchoolManagementDbContext context, IReadRepository readRepo, IMapper mapper)
    {
        _context = context;
        _readRepo = readRepo;
        _mapper = mapper;
    }

    public async Task<CountryResponse> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Countries
            .AnyAsync(c => c.Name == request.Name || c.Code == request.Code, cancellationToken);

        if (exists)
            throw new InvalidOperationException(AppMessages.Country.AlreadyExists(request.Name, request.Code));

        var country = new Country
        {
            Name = request.Name,
            Code = request.Code,
        };

        await _context.Countries.AddAsync(country, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CountryResponse>(country);
    }

    public async Task<CountryResponse> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.Country.NotFound(id));

        country.Name = request.Name;
        country.Code = request.Code;
        country.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CountryResponse>(country);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException(AppMessages.Country.NotFound(id));

        country.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CountryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _readRepo.QueryFirstOrDefaultAsync<CountryResponse>(
            CountryQueries.GetById,
            new { Id = id });
    }

    public async Task<PagedResult<CountryResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
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
            CountryQueries.GetAll,
            pagination.SortBy, pagination.SortDescending,
            CountryQueries.AllowedSortColumns, CountryQueries.DefaultSortColumn);

        return await _readRepo.QueryPagedAsync<CountryResponse>(
            dataSql,
            CountryQueries.CountAll,
            param,
            pagination.Page,
            pagination.PageSize);
    }
}
