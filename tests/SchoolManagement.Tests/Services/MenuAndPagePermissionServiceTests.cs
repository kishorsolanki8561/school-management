using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class MenuAndPagePermissionServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext      _context;
    private readonly Mock<IReadRepository>          _readRepoMock = new();
    private readonly MenuAndPagePermissionService   _sut;

    public MenuAndPagePermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _sut     = new MenuAndPagePermissionService(_context, _readRepoMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_TogglesIsAllowed_FalseToTrue()
    {
        var perm = await SeedPermissionAsync(isAllowed: false);

        var result = await _sut.UpdateAsync(perm.Id, perm.RoleId);

        result.IsAllowed.Should().BeTrue();
        (await _context.MenuAndPagePermissions.FindAsync(perm.Id))!.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_TogglesIsAllowed_TrueToFalse()
    {
        var perm = await SeedPermissionAsync(isAllowed: true);

        var result = await _sut.UpdateAsync(perm.Id, perm.RoleId);

        result.IsAllowed.Should().BeFalse();
        (await _context.MenuAndPagePermissions.FindAsync(perm.Id))!.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WrongRoleId_ThrowsKeyNotFoundException()
    {
        var perm = await SeedPermissionAsync(isAllowed: false);

        var act = () => _sut.UpdateAsync(perm.Id, roleId: 999);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, roleId: 1);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToReadRepository()
    {
        var expected = new MenuAndPagePermissionResponse { Id = 1, RoleId = 2, IsAllowed = true };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<MenuAndPagePermissionResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.IsAllowed.Should().BeTrue();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaged()
    {
        var expected = PagedResult<MenuAndPagePermissionResponse>.Create(
            new[] { new MenuAndPagePermissionResponse { Id = 1, IsAllowed = false } },
            total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<MenuAndPagePermissionResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_PassesFilterParamsToRepo()
    {
        object? capturedParam = null;
        _readRepoMock
            .Setup(r => r.QueryPagedAsync<MenuAndPagePermissionResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<string, string, object, int, int>((_, _, p, _, _) => capturedParam = p)
            .ReturnsAsync(PagedResult<MenuAndPagePermissionResponse>.Create(Array.Empty<MenuAndPagePermissionResponse>(), 0, 1, 20));

        await _sut.GetAllAsync(new PaginationRequest(), menuId: 3, pageId: 7, roleId: 5);

        capturedParam.Should().NotBeNull();
        var props = capturedParam!.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(capturedParam));
        props["MenuId"].Should().Be(3);
        props["PageId"].Should().Be(7);
        props["RoleId"].Should().Be(5);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MenuAndPagePermission> SeedPermissionAsync(bool isAllowed)
    {
        var perm = new MenuAndPagePermission
        {
            MenuId       = 1,
            PageId       = 1,
            PageModuleId = 1,
            ActionId     = ActionType.VIEW_LIST,
            RoleId       = 1,
            IsAllowed    = isAllowed,
        };
        await _context.MenuAndPagePermissions.AddAsync(perm);
        await _context.SaveChangesAsync();
        return perm;
    }
}
