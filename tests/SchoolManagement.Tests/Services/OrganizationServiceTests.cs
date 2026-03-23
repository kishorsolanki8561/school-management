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

public sealed class OrganizationServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper _mapper;
    private readonly OrganizationService _sut;

    public OrganizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _mapper  = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut     = new OrganizationService(_context, _readRepoMock.Object, _mapper);
    }

    public void Dispose() => _context.Dispose();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsOrganizationResponse()
    {
        var request = new CreateOrganizationRequest { Name = "Sunrise Academy", Address = "123 Main St" };

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("Sunrise Academy");
        result.Address.Should().Be("123 Main St");
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsToDatabase()
    {
        var request = new CreateOrganizationRequest { Name = "Green Valley School" };

        await _sut.CreateAsync(request);

        var saved = await _context.Organizations.FirstOrDefaultAsync(o => o.Name == "Green Valley School");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await SeedOrganizationAsync("Horizon Institute");

        var act = () => _sut.CreateAsync(new CreateOrganizationRequest { Name = "Horizon Institute" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var org = await SeedOrganizationAsync("Old Name");

        var result = await _sut.UpdateAsync(org.Id, new UpdateOrganizationRequest
        {
            Name     = "New Name",
            Address  = "456 New Ave",
            IsActive = false,
        });

        result.Name.Should().Be("New Name");
        result.Address.Should().Be("456 New Ave");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateOrganizationRequest { Name = "X", IsActive = true });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletes()
    {
        var org = await SeedOrganizationAsync("Delete Me");

        await _sut.DeleteAsync(org.Id);

        var deleted = await _context.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == org.Id);

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
        var expected = new OrganizationResponse { Id = 1, Name = "City School", IsActive = true };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrganizationResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("City School");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrganizationResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((OrganizationResponse?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResultFromReadRepository()
    {
        var expected = PagedResult<OrganizationResponse>.Create(
            new[] { new OrganizationResponse { Id = 1, Name = "Sunrise Academy" } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<OrganizationResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Organization> SeedOrganizationAsync(string name, string? address = null)
    {
        var org = new Organization { Name = name, Address = address };
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();
        return org;
    }
}
