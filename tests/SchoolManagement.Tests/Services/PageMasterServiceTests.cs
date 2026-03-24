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

/// <summary>
/// Uses SQLite in-memory (via SqliteServiceTestBase) because PageMasterService
/// wraps multi-step operations in IDbContextTransaction, which is not supported
/// by the EF Core InMemory provider.
/// </summary>
public sealed class PageMasterServiceTests : SqliteServiceTestBase
{
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly IMapper               _mapper;
    private readonly PageMasterService     _sut;

    public PageMasterServiceTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>()).CreateMapper();
        _sut    = new PageMasterService(_context, _readRepoMock.Object, _mapper);
    }

    // ── Create — happy paths ──────────────────────────────────────────────────

    [Fact]
    public async Task CreatePageAsync_WithModuleAndExplicitActions_PersistsAll()
    {
        var menu = await SeedMenuAsync("Admin", hasChild: true);
        await SeedRolesAsync(3);

        var result = await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Users",
            PageUrl = "/users",
            MenuId  = menu.Id,
            Modules = new[]
            {
                new CreatePageModuleInput
                {
                    Name    = "User CRUD",
                    Actions = new[] { ActionType.ADD, ActionType.EDIT },
                },
            },
        });

        result.Name.Should().Be("Users");

        _context.PageMasterModuleActionMappings.Count().Should().Be(2);
        _context.MenuAndPagePermissions.Count().Should().Be(2 * 3);   // 2 actions × 3 roles
        _context.MenuAndPagePermissions.Should().AllSatisfy(p => p.IsAllowed.Should().BeFalse());
    }

    [Fact]
    public async Task CreatePageAsync_WithModuleAndNoActions_DefaultsToAllActionTypes()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        await SeedRolesAsync(2);

        await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Page",
            PageUrl = "/page",
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod" } },
        });

        var actionCount = Enum.GetValues<ActionType>().Length;   // 7

        _context.PageMasterModuleActionMappings.Count().Should().Be(actionCount);
        _context.MenuAndPagePermissions.Count().Should().Be(actionCount * 2);
    }

    [Fact]
    public async Task CreatePageAsync_WithMultipleModules_AllPersisted()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        await SeedRolesAsync(1);

        await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Page",
            PageUrl = "/page",
            MenuId  = menu.Id,
            Modules = new[]
            {
                new CreatePageModuleInput { Name = "Mod A" },
                new CreatePageModuleInput { Name = "Mod B" },
            },
        });

        var actionCount = Enum.GetValues<ActionType>().Length;
        _context.PageMasterModules.Count().Should().Be(2);
        _context.PageMasterModuleActionMappings.Count().Should().Be(2 * actionCount);
    }

    [Fact]
    public async Task CreatePageAsync_HasChildFalse_NoPage_Allowed()
    {
        var menu = await SeedMenuAsync("Leaf", hasChild: false);

        var act = () => _sut.CreatePageAsync(new CreatePageRequest
        {
            Name = "Only", PageUrl = "/only", MenuId = menu.Id,
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreatePageAsync_HasChildFalse_PageExists_ThrowsInvalidOperationException()
    {
        var menu = await SeedMenuAsync("Leaf", hasChild: false);
        await SeedPageAsync("First", menu.Id);

        var act = () => _sut.CreatePageAsync(new CreatePageRequest
        {
            Name = "Second", PageUrl = "/second", MenuId = menu.Id,
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreatePageAsync_MenuNotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.CreatePageAsync(new CreatePageRequest
        {
            Name = "X", PageUrl = "/x", MenuId = 999,
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreatePageAsync_DuplicateAction_SkipsInsert()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        await SeedRolesAsync(2);

        // First page — creates 1 action + 2 permission rows
        await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Page 1",
            PageUrl = "/p1",
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod", Actions = new[] { ActionType.ADD } } },
        });

        // Second page (HasChild=true allows it) — 1 action + 2 permission rows on different page
        await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Page 2",
            PageUrl = "/p2",
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod", Actions = new[] { ActionType.ADD } } },
        });

        // Each page has exactly 1 action mapping → total 2
        _context.PageMasterModuleActionMappings.Count().Should().Be(2);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePageAsync_ValidScalarFields_UpdatesPage()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        var page = await SeedPageAsync("Old", menu.Id);

        var result = await _sut.UpdatePageAsync(page.Id, new UpdatePageRequest
        {
            Name     = "New",
            PageUrl  = "/new",
            MenuId   = menu.Id,
            IsActive = false,
        });

        result.Name.Should().Be("New");
        result.IsActive.Should().BeFalse();

        var db = await _context.PageMasters.FindAsync(page.Id);
        db!.Name.Should().Be("New");
        db.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePageAsync_WithNewModule_AddsModuleAndPermissions()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        var page = await SeedPageAsync("Page", menu.Id);
        await SeedRolesAsync(2);

        await _sut.UpdatePageAsync(page.Id, new UpdatePageRequest
        {
            Name    = page.Name,
            PageUrl = page.PageUrl,
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "New Module" } },
        });

        _context.PageMasterModules.Count().Should().Be(1);
        _context.MenuAndPagePermissions.Count().Should().Be(Enum.GetValues<ActionType>().Length * 2);
    }

    [Fact]
    public async Task UpdatePageAsync_DuplicateModuleName_SkipsInsert()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        var page = await SeedPageAsync("Page", menu.Id);
        await SeedRolesAsync(1);

        // First update: creates "Mod"
        await _sut.UpdatePageAsync(page.Id, new UpdatePageRequest
        {
            Name    = page.Name,
            PageUrl = page.PageUrl,
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod" } },
        });

        var countAfterFirst = _context.PageMasterModules.Count();

        // Second update: same module name → should reuse existing module
        await _sut.UpdatePageAsync(page.Id, new UpdatePageRequest
        {
            Name    = page.Name,
            PageUrl = page.PageUrl,
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod" } },
        });

        _context.PageMasterModules.Count().Should().Be(countAfterFirst);
    }

    [Fact]
    public async Task UpdatePageAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdatePageAsync(999, new UpdatePageRequest { Name = "X", PageUrl = "/x", MenuId = 1 });
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePageAsync_CascadeSoftDeletesModulesActionsPermissions()
    {
        var menu = await SeedMenuAsync("Menu", hasChild: true);
        await SeedRolesAsync(2);

        var page = await _sut.CreatePageAsync(new CreatePageRequest
        {
            Name    = "Page",
            PageUrl = "/page",
            MenuId  = menu.Id,
            Modules = new[] { new CreatePageModuleInput { Name = "Mod" } },
        });

        await _sut.DeletePageAsync(page.Id);

        var deletedPage = await _context.PageMasters.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == page.Id);
        deletedPage!.IsDeleted.Should().BeTrue();

        var modules = await _context.PageMasterModules.IgnoreQueryFilters()
            .Where(m => m.PageId == page.Id).ToListAsync();
        modules.Should().NotBeEmpty();
        modules.Should().AllSatisfy(m => m.IsDeleted.Should().BeTrue());

        var actions = await _context.PageMasterModuleActionMappings.IgnoreQueryFilters()
            .Where(a => a.PageId == page.Id).ToListAsync();
        actions.Should().NotBeEmpty();
        actions.Should().AllSatisfy(a => a.IsDeleted.Should().BeTrue());

        var permissions = await _context.MenuAndPagePermissions.IgnoreQueryFilters()
            .Where(p => p.PageId == page.Id).ToListAsync();
        permissions.Should().NotBeEmpty();
        permissions.Should().AllSatisfy(p => p.IsDeleted.Should().BeTrue());
    }

    // ── GetAll Pages ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPagesAsync_ReturnsPaged()
    {
        var expected = PagedResult<PageResponse>.Create(
            new[] { new PageResponse { Id = 1, Name = "Dashboard" } }, total: 1, page: 1, pageSize: 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<PageResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAllPagesAsync(new PaginationRequest());
        result.Items.Should().HaveCount(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MenuMaster> SeedMenuAsync(string name, bool hasChild)
    {
        var menu = new MenuMaster { Name = name, HasChild = hasChild, Position = 1 };
        await _context.MenuMasters.AddAsync(menu);
        await _context.SaveChangesAsync();
        return menu;
    }

    private async Task<PageMaster> SeedPageAsync(string name, int menuId)
    {
        var page = new PageMaster
        {
            Name    = name,
            PageUrl = "/" + name.ToLower().Replace(" ", "-"),
            MenuId  = menuId,
        };
        await _context.PageMasters.AddAsync(page);
        await _context.SaveChangesAsync();
        return page;
    }

    private async Task SeedRolesAsync(int count)
    {
        var roles = Enumerable.Range(1, count)
            .Select(i => new Role { Id = i, Name = $"Role{i}", IsOrgRole = false })
            .ToList();
        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();
    }
}
