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

public sealed class CountryServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper _mapper;
    private readonly CountryService _sut;

    public CountryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut = new CountryService(_context, _readRepoMock.Object, _mapper);
    }

    public void Dispose() => _context.Dispose();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsCountryResponse()
    {
        var request = new CreateCountryRequest { Name = "India", Code = "IND" };

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("India");
        result.Code.Should().Be("IND");
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsToDatabase()
    {
        var request = new CreateCountryRequest { Name = "Germany", Code = "DEU" };

        await _sut.CreateAsync(request);

        var saved = await _context.Countries.FirstOrDefaultAsync(c => c.Name == "Germany");
        saved.Should().NotBeNull();
        saved!.Code.Should().Be("DEU");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await SeedCountryAsync("France", "FRA");

        var act = () => _sut.CreateAsync(new CreateCountryRequest { Name = "France", Code = "FR2" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        await SeedCountryAsync("France", "FRA");

        var act = () => _sut.CreateAsync(new CreateCountryRequest { Name = "FranceNew", Code = "FRA" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var country = await SeedCountryAsync("Old Name", "OLD");

        var result = await _sut.UpdateAsync(country.Id, new UpdateCountryRequest
        {
            Name = "New Name",
            Code = "NEW",
            IsActive = false,
        });

        result.Name.Should().Be("New Name");
        result.Code.Should().Be("NEW");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateCountryRequest { Name = "X", Code = "XXX", IsActive = true });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletes()
    {
        var country = await SeedCountryAsync("Australia", "AUS");

        await _sut.DeleteAsync(country.Id);

        var deleted = await _context.Countries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == country.Id);

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
        var expected = new CountryResponse { Id = 1, Name = "Canada", Code = "CAN", IsActive = true };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Canada");
        result.Code.Should().Be("CAN");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((CountryResponse?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultFromReadRepository()
    {
        var expected = PagedResult<CountryResponse>.Create(
            new[] { new CountryResponse { Id = 1, Name = "India", Code = "IND" } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<CountryResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Country> SeedCountryAsync(string name, string code)
    {
        var country = new Country { Name = name, Code = code };
        await _context.Countries.AddAsync(country);
        await _context.SaveChangesAsync();
        return country;
    }
}
