using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Models.Mappings;
using SchoolManagement.Services.Implementations;
using SchoolManagement.Tests.Infrastructure;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class MenuMasterServiceTests : SqliteServiceTestBase
{
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper               _mapper;
    private readonly MenuMasterService     _sut;

    public MenuMasterServiceTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut    = new MenuMasterService(_context, _readRepoMock.Object, _mapper);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndReturnsResponse()
    {
        var request = new CreateMenuRequest { Name = "Dashboard", HasChild = false, Position = 1, IconClass = "fa-home" };

        var result = await _sut.CreateAsync(request);

        result.Name.Should().Be("Dashboard");
        result.Position.Should().Be(1);
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);

        var saved = await _context.MenuMasters.FirstOrDefaultAsync(m => m.Name == "Dashboard");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_SameNameDifferentParents_AllowedNoDuplicateConstraint()
    {
        var parent1 = await SeedMenuAsync("Parent A");
        var parent2 = await SeedMenuAsync("Parent B");

        // Same child name under two different parents — must be allowed
        var r1 = await _sut.CreateAsync(new CreateMenuRequest { Name = "Reports", ParentMenuId = parent1.Id, Position = 1 });
        var r2 = await _sut.CreateAsync(new CreateMenuRequest { Name = "Reports", ParentMenuId = parent2.Id, Position = 1 });

        r1.Id.Should().NotBe(r2.Id);
        _context.MenuMasters.Count(m => m.Name == "Reports").Should().Be(2);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var menu = await SeedMenuAsync("Old Menu");

        var result = await _sut.UpdateAsync(menu.Id, new UpdateMenuRequest
        {
            Name     = "New Menu",
            HasChild = true,
            Position = 5,
            IsActive = false,
        });

        result.Name.Should().Be("New Menu");
        result.HasChild.Should().BeTrue();
        result.Position.Should().Be(5);
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateMenuRequest { Name = "X", Position = 1 });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletes()
    {
        var menu = await SeedMenuAsync("Delete Me");

        await _sut.DeleteAsync(menu.Id);

        var deleted = await _context.MenuMasters.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == menu.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.DeleteAsync(999);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_CascadeSoftDeletesAllChildRecords()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var menu = await SeedMenuAsync("Finance", hasChild: true);

        var page = new PageMaster { Name = "Invoices", PageUrl = "/invoices", MenuId = menu.Id };
        await _context.PageMasters.AddAsync(page);
        await _context.SaveChangesAsync();

        var module = new PageMasterModule { Name = "InvoiceList", PageId = page.Id };
        await _context.PageMasterModules.AddAsync(module);
        await _context.SaveChangesAsync();

        var action = new PageMasterModuleActionMapping
        {
            PageId       = page.Id,
            PageModuleId = module.Id,
            ActionId     = ActionType.VIEW_LIST,
        };
        await _context.PageMasterModuleActionMappings.AddAsync(action);

        // Role row required by FK on MenuAndPagePermissions.RoleId
        var role = new Role { Id = (int)UserRole.OwnerAdmin, Name = UserRole.OwnerAdmin.ToString() };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var permission = new MenuAndPagePermission
        {
            MenuId       = menu.Id,
            PageId       = page.Id,
            PageModuleId = module.Id,
            ActionId     = ActionType.VIEW_LIST,
            RoleId       = role.Id,
            IsAllowed    = false,
        };
        await _context.MenuAndPagePermissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        // ── Act ───────────────────────────────────────────────────────────────
        await _sut.DeleteAsync(menu.Id);

        // ── Assert — every child row must be soft-deleted ─────────────────────
        var deletedMenu = await _context.MenuMasters
            .IgnoreQueryFilters().FirstAsync(m => m.Id == menu.Id);
        deletedMenu.IsDeleted.Should().BeTrue();

        var deletedPage = await _context.PageMasters
            .IgnoreQueryFilters().FirstAsync(p => p.Id == page.Id);
        deletedPage.IsDeleted.Should().BeTrue();

        var deletedModule = await _context.PageMasterModules
            .IgnoreQueryFilters().FirstAsync(m => m.Id == module.Id);
        deletedModule.IsDeleted.Should().BeTrue();

        var deletedAction = await _context.PageMasterModuleActionMappings
            .IgnoreQueryFilters().FirstAsync(a => a.Id == action.Id);
        deletedAction.IsDeleted.Should().BeTrue();

        var deletedPermission = await _context.MenuAndPagePermissions
            .IgnoreQueryFilters().FirstAsync(p => p.Id == permission.Id);
        deletedPermission.IsDeleted.Should().BeTrue();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToReadRepository()
    {
        var expected = new MenuResponse { Id = 1, Name = "Dashboard" };
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<MenuResponse>(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Dashboard");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPaged()
    {
        var expected = PagedResult<MenuResponse>.Create(
            new[] { new MenuResponse { Id = 1, Name = "Dashboard" } }, total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<MenuResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllAsync(new PaginationRequest());

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MenuMaster> SeedMenuAsync(string name, bool hasChild = false)
    {
        var menu = new MenuMaster { Name = name, HasChild = hasChild, Position = 1 };
        await _context.MenuMasters.AddAsync(menu);
        await _context.SaveChangesAsync();
        return menu;
    }
}
