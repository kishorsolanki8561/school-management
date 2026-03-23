using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Mappings;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class CityServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper _mapper;
    private readonly CityService _sut;

    public CityServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut = new CityService(_context, _readRepoMock.Object, _mapper);
    }

    public void Dispose() => _context.Dispose();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCityResponse()
    {
        var (_, state) = await SeedCountryAndStateAsync();

        var result = await _sut.CreateAsync(new CreateCityRequest
        {
            Name = "Ahmedabad",
            StateId = state.Id,
        });

        result.Name.Should().Be("Ahmedabad");
        result.StateId.Should().Be(state.Id);
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var (_, state) = await SeedCountryAndStateAsync();

        await _sut.CreateAsync(new CreateCityRequest { Name = "Surat", StateId = state.Id });

        var saved = await _context.Cities.FirstOrDefaultAsync(c => c.Name == "Surat");
        saved.Should().NotBeNull();
        saved!.StateId.Should().Be(state.Id);
    }

    [Fact]
    public async Task CreateAsync_InvalidStateId_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.CreateAsync(new CreateCityRequest { Name = "Ghost City", StateId = 999 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameState_ThrowsInvalidOperationException()
    {
        var (_, state) = await SeedCountryAndStateAsync();
        await SeedCityAsync("Ahmedabad", state.Id);

        var act = () => _sut.CreateAsync(new CreateCityRequest { Name = "Ahmedabad", StateId = state.Id });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_SameNameDifferentState_Succeeds()
    {
        var country = await SeedCountryAsync("India", "IND");
        var state1 = await SeedStateAsync("Gujarat", "GJ", country.Id);
        var state2 = await SeedStateAsync("Maharashtra", "MH", country.Id);
        await SeedCityAsync("Mumbai", state2.Id);

        // "Mumbai" in a different state should succeed
        var result = await _sut.CreateAsync(new CreateCityRequest { Name = "Mumbai", StateId = state1.Id });

        result.Name.Should().Be("Mumbai");
        result.StateId.Should().Be(state1.Id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var (_, state) = await SeedCountryAndStateAsync();
        var city = await SeedCityAsync("Old City", state.Id);

        var result = await _sut.UpdateAsync(city.Id, new UpdateCityRequest
        {
            Name = "New City",
            IsActive = false,
        });

        result.Name.Should().Be("New City");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateCityRequest { Name = "X", IsActive = true });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletes()
    {
        var (_, state) = await SeedCountryAndStateAsync();
        var city = await SeedCityAsync("Vadodara", state.Id);

        await _sut.DeleteAsync(city.Id);

        var deleted = await _context.Cities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == city.Id);

        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.DeleteAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsFromReadRepository()
    {
        var expected = new CityResponse
        {
            Id = 1, Name = "Ahmedabad", StateId = 1, StateName = "Gujarat",
            CountryId = 1, CountryName = "India",
        };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<CityResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Ahmedabad");
        result.StateName.Should().Be("Gujarat");
        result.CountryName.Should().Be("India");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<CityResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((CityResponse?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultFromReadRepository()
    {
        var expected = PagedResult<CityResponse>.Create(
            new[] { new CityResponse { Id = 1, Name = "Ahmedabad", StateId = 1 } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<CityResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    // ── GetByState ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStateAsync_ReturnsFilteredPagedResult()
    {
        var expected = PagedResult<CityResponse>.Create(
            new[] { new CityResponse { Id = 1, Name = "Ahmedabad", StateId = 7 } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<CityResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByStateAsync(7, new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Items.First().StateId.Should().Be(7);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Country country, State state)> SeedCountryAndStateAsync()
    {
        var country = await SeedCountryAsync("India", "IND");
        var state = await SeedStateAsync("Gujarat", "GJ", country.Id);
        return (country, state);
    }

    private async Task<Country> SeedCountryAsync(string name, string code)
    {
        var country = new Country { Name = name, Code = code };
        await _context.Countries.AddAsync(country);
        await _context.SaveChangesAsync();
        return country;
    }

    private async Task<State> SeedStateAsync(string name, string code, int countryId)
    {
        var state = new State { Name = name, Code = code, CountryId = countryId };
        await _context.States.AddAsync(state);
        await _context.SaveChangesAsync();
        return state;
    }

    private async Task<City> SeedCityAsync(string name, int stateId)
    {
        var city = new City { Name = name, StateId = stateId };
        await _context.Cities.AddAsync(city);
        await _context.SaveChangesAsync();
        return city;
    }
}
