using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Audit;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories;
using SchoolManagement.Models.Entities;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class DapperAuditExecutorTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IDapperHelper>       _dapperMock       = new();
    private readonly Mock<IRequestContext>     _requestContextMock = new();
    private readonly DapperAuditExecutor       _sut;

    public DapperAuditExecutorTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);

        _requestContextMock.Setup(r => r.UserId).Returns("1");
        _requestContextMock.Setup(r => r.Username).Returns("admin");

        _sut = new DapperAuditExecutor(_dapperMock.Object, _context, _requestContextMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── Skip conditions ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NullAuditContext_DoesNotSaveAuditLog()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        await _sut.ExecuteAsync("UPDATE ...", null, null);

        _context.AuditLogs.Count().Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroRowsAffected_DoesNotSaveAuditLog()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(0);

        await _sut.ExecuteAsync("UPDATE ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "1",
            Action    = "Updated",
            NewEntity = new Organization { Name = "Test" },
        });

        _context.AuditLogs.Count().Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_TableNotInConfig_DoesNotSaveAuditLog()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        await _sut.ExecuteAsync("INSERT ...", null, new DapperAuditContext
        {
            TableName = "UnknownTable",
            EntityId  = "1",
            Action    = "Created",
            NewEntity = new { Name = "Test" },
        });

        _context.AuditLogs.Count().Should().Be(0);
    }

    // ── Created ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CreatedAction_SavesNewDataAuditLog()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        var entity = new Organization { Id = 5, Name = "Sunrise Academy", Address = "123 Main St", IsActive = true };

        await _sut.ExecuteAsync("INSERT ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "5",
            Action    = "Created",
            NewEntity = entity,
        });

        var log = _context.AuditLogs.Single();
        log.Action.Should().Be("Created");
        log.EntityId.Should().Be("5");
        log.OldData.Should().BeNull();
        log.NewData.Should().Contain("Sunrise Academy");
    }

    // ── Updated ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_UpdatedAction_SavesOnlyChangedColumns()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        var before = new Organization { Id = 5, Name = "Old Name",  Address = "Same St", IsActive = true };
        var after  = new Organization { Id = 5, Name = "New Name",  Address = "Same St", IsActive = true };

        await _sut.ExecuteAsync("UPDATE ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "5",
            Action    = "Updated",
            OldEntity = before,
            NewEntity = after,
        });

        var log = _context.AuditLogs.Single();
        log.Action.Should().Be("Updated");
        log.OldData.Should().Contain("Old Name");
        log.NewData.Should().Contain("New Name");
        // unchanged Address must NOT appear
        log.OldData.Should().NotContain("Same St");
        log.NewData.Should().NotContain("Same St");
    }

    [Fact]
    public async Task ExecuteAsync_UpdatedAction_NothingChanged_DoesNotSaveAuditLog()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        var snapshot = new Organization { Id = 5, Name = "Same", Address = "Same", IsActive = true };

        await _sut.ExecuteAsync("UPDATE ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "5",
            Action    = "Updated",
            OldEntity = snapshot,
            NewEntity = snapshot,
        });

        _context.AuditLogs.Count().Should().Be(0);
    }

    // ── Bool formatting ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_BooleanColumn_FormatsAsYesNo()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        var entity = new Organization { Id = 1, Name = "Test Org", IsActive = true };

        await _sut.ExecuteAsync("INSERT ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "1",
            Action    = "Created",
            NewEntity = entity,
        });

        var log = _context.AuditLogs.Single();
        log.NewData.Should().Contain("Yes");   // IsActive = true → "Yes"
        log.NewData.Should().NotContain("True");
    }

    // ── Null column skipped ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NullColumnValue_IsNotIncludedInAuditData()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        // Address is null — should not appear in NewData
        var entity = new Organization { Id = 2, Name = "Sunrise School", Address = null, IsActive = true };

        await _sut.ExecuteAsync("INSERT ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "2",
            Action    = "Created",
            NewEntity = entity,
        });

        var log = _context.AuditLogs.Single();
        log.NewData.Should().NotContain("Address");
    }

    // ── ModifiedBy and request context stamped ────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_StampsModifiedByAndCreatedByFromRequestContext()
    {
        _dapperMock.Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object?>())).ReturnsAsync(1);

        var entity = new Organization { Id = 3, Name = "School", IsActive = true };

        await _sut.ExecuteAsync("INSERT ...", null, new DapperAuditContext
        {
            TableName = "Organizations",
            EntityId  = "3",
            Action    = "Created",
            NewEntity = entity,
        });

        var log = _context.AuditLogs.Single();
        log.ModifiedBy.Should().Be("1");
        log.CreatedBy.Should().Be("admin");
    }
}
