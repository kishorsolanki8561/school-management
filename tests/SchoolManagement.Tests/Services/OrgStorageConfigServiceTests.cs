using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class OrgStorageConfigServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext    _context;
    private readonly Mock<IRequestContext>        _requestCtxMock = new();
    private readonly OrgStorageConfigService      _sut;

    public OrgStorageConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _requestCtxMock.Setup(r => r.OrgId).Returns(1);

        _sut = new OrgStorageConfigService(_context, _requestCtxMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NewConfig_PersistsAndReturnsResponse()
    {
        var request = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.HostingServer,
            BasePath: "/var/uploads",
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: null, ConnectionString: null);

        var result = await _sut.SaveAsync(request);

        result.OrgId.Should().Be(1);
        result.StorageType.Should().Be(StorageType.HostingServer);
        result.BasePath.Should().Be("/var/uploads");
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveAsync_ExistingConfig_UpdatesExisting()
    {
        var first = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.HostingServer,
            BasePath: "/old-path",
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: null, ConnectionString: null);
        await _sut.SaveAsync(first);

        var second = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.AWSS3,
            BasePath: null,
            BucketName: "my-bucket", Region: "ap-south-1", AccessKey: "KEY", SecretKey: "SECRET",
            ContainerName: null, ConnectionString: null);
        var result = await _sut.SaveAsync(second);

        result.StorageType.Should().Be(StorageType.AWSS3);
        result.BucketName.Should().Be("my-bucket");

        var count = await _context.OrgStorageConfigs.CountAsync();
        count.Should().Be(1); // still only one row
    }

    [Fact]
    public async Task SaveAsync_SoftDeletedConfig_RestoresOnUpsert()
    {
        var request = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.HostingServer,
            BasePath: "/path",
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: null, ConnectionString: null);
        await _sut.SaveAsync(request);
        await _sut.DeleteAsync(1);

        var result = await _sut.SaveAsync(request);

        result.IsActive.Should().BeTrue();
        var count = await _context.OrgStorageConfigs.IgnoreQueryFilters().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task SaveAsync_NullOrgId_ThrowsInvalidOperationException()
    {
        _requestCtxMock.Setup(r => r.OrgId).Returns((int?)null);

        var request = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.HostingServer,
            BasePath: "/path",
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: null, ConnectionString: null);

        var act = () => _sut.SaveAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── GetByOrgIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByOrgIdAsync_Exists_ReturnsResponse()
    {
        var request = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.AzureBlob,
            BasePath: null,
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: "my-container", ConnectionString: "DefaultEndpoints=...");
        await _sut.SaveAsync(request);

        var result = await _sut.GetByOrgIdAsync(1);

        result.Should().NotBeNull();
        result!.StorageType.Should().Be(StorageType.AzureBlob);
        result.ContainerName.Should().Be("my-container");
    }

    [Fact]
    public async Task GetByOrgIdAsync_NotExists_ReturnsNull()
    {
        var result = await _sut.GetByOrgIdAsync(999);
        result.Should().BeNull();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Exists_SoftDeletes()
    {
        var request = new SaveOrgStorageConfigRequest(
            StorageType: StorageType.HostingServer,
            BasePath: "/path",
            BucketName: null, Region: null, AccessKey: null, SecretKey: null,
            ContainerName: null, ConnectionString: null);
        await _sut.SaveAsync(request);

        await _sut.DeleteAsync(1);

        var result = await _sut.GetByOrgIdAsync(1);
        result.Should().BeNull(); // hidden by query filter

        var deleted = await _context.OrgStorageConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrgId == 1);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }
}
