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

public sealed class StateServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper _mapper;
    private readonly StateService _sut;

    public StateServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut = new StateService(_context, _readRepoMock.Object, _mapper);
    }

    public void Dispose() => _context.Dispose();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsStateResponse()
    {
        var country = await SeedCountryAsync("India", "IND");

        var result = await _sut.CreateAsync(new CreateStateRequest
        {
            Name = "Gujarat",
            Code = "GJ",
            CountryId = country.Id,
        });

        result.Name.Should().Be("Gujarat");
        result.Code.Should().Be("GJ");
        result.CountryId.Should().Be(country.Id);
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var country = await SeedCountryAsync("India", "IND");

        await _sut.CreateAsync(new CreateStateRequest { Name = "Maharashtra", Code = "MH", CountryId = country.Id });

        var saved = await _context.States.FirstOrDefaultAsync(s => s.Name == "Maharashtra");
        saved.Should().NotBeNull();
        saved!.CountryId.Should().Be(country.Id);
    }

    [Fact]
    public async Task CreateAsync_InvalidCountryId_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.CreateAsync(new CreateStateRequest
        {
            Name = "Unknown State",
            Code = "UNK",
            CountryId = 999,
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameCountry_ThrowsInvalidOperationException()
    {
        var country = await SeedCountryAsync("India", "IND");
        await SeedStateAsync("Gujarat", "GJ", country.Id);

        var act = () => _sut.CreateAsync(new CreateStateRequest { Name = "Gujarat", Code = "GJ2", CountryId = country.Id });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_SameNameDifferentCountry_Succeeds()
    {
        var india = await SeedCountryAsync("India", "IND");
        var usa = await SeedCountryAsync("United States", "USA");
        await SeedStateAsync("California", "CA", usa.Id);

        // A state named "California" in India should be allowed
        var result = await _sut.CreateAsync(new CreateStateRequest
        {
            Name = "California",
            Code = "CA-IN",
            CountryId = india.Id,
        });

        result.Name.Should().Be("California");
        result.CountryId.Should().Be(india.Id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var country = await SeedCountryAsync("India", "IND");
        var state = await SeedStateAsync("Old State", "OLD", country.Id);

        var result = await _sut.UpdateAsync(state.Id, new UpdateStateRequest
        {
            Name = "New State",
            Code = "NEW",
            IsActive = false,
        });

        result.Name.Should().Be("New State");
        result.Code.Should().Be("NEW");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateStateRequest { Name = "X", Code = "XX", IsActive = true });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletes()
    {
        var country = await SeedCountryAsync("India", "IND");
        var state = await SeedStateAsync("Rajasthan", "RJ", country.Id);

        await _sut.DeleteAsync(state.Id);

        var deleted = await _context.States
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == state.Id);

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
        var expected = new StateResponse { Id = 1, Name = "Gujarat", Code = "GJ", CountryId = 1, CountryName = "India" };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<StateResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Gujarat");
        result.CountryName.Should().Be("India");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<StateResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((StateResponse?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultFromReadRepository()
    {
        var expected = PagedResult<StateResponse>.Create(
            new[] { new StateResponse { Id = 1, Name = "Gujarat", CountryId = 1 } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<StateResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    // ── GetByCountry ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCountryAsync_ReturnsFilteredPagedResult()
    {
        var expected = PagedResult<StateResponse>.Create(
            new[] { new StateResponse { Id = 1, Name = "Gujarat", CountryId = 5 } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<StateResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCountryAsync(5, new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Items.First().CountryId.Should().Be(5);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
}
