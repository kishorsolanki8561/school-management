using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class OrgFileUploadConfigServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext      _context;
    private readonly Mock<IReadRepository>          _readRepoMock = new();
    private readonly OrgFileUploadConfigService     _sut;

    public OrgFileUploadConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _sut     = new OrgFileUploadConfigService(_context, _readRepoMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndReturnsResponse()
    {
        var request = new CreateOrgFileUploadConfigRequest
        {
            OrgId             = 1,
            PageId            = 2,
            AllowedExtensions = ".pdf,.jpg",
            AllowedMimeTypes  = "application/pdf,image/jpeg",
            MaxFileSizeBytes  = 2_097_152,
            AllowMultiple     = true,
        };

        var result = await _sut.CreateAsync(request);

        result.OrgId.Should().Be(1);
        result.PageId.Should().Be(2);
        result.AllowedExtensions.Should().Be(".pdf,.jpg");
        result.AllowedMimeTypes.Should().Be("application/pdf,image/jpeg");
        result.MaxFileSizeBytes.Should().Be(2_097_152);
        result.AllowMultiple.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);

        var saved = await _context.OrgFileUploadConfigs.FirstOrDefaultAsync(c => c.OrgId == 1 && c.PageId == 2);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateOrgAndPage_ThrowsInvalidOperationException()
    {
        await SeedConfigAsync(orgId: 3, pageId: 4);

        var request = new CreateOrgFileUploadConfigRequest
        {
            OrgId  = 3,
            PageId = 4,
            AllowedExtensions = ".png",
            AllowedMimeTypes  = "image/png",
            MaxFileSizeBytes  = 1_000_000,
        };

        var act = () => _sut.CreateAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingConfig_UpdatesFieldsAndReturnsResponse()
    {
        var config = await SeedConfigAsync(orgId: 5, pageId: 6,
            extensions: ".pdf", mimeTypes: "application/pdf", maxBytes: 1_000_000, allowMultiple: false);

        var updateRequest = new UpdateOrgFileUploadConfigRequest
        {
            AllowedExtensions = ".jpg,.png",
            AllowedMimeTypes  = "image/jpeg,image/png",
            MaxFileSizeBytes  = 3_000_000,
            AllowMultiple     = true,
            IsActive          = true,
        };

        var result = await _sut.UpdateAsync(config.Id, updateRequest);

        result.AllowedExtensions.Should().Be(".jpg,.png");
        result.AllowedMimeTypes.Should().Be("image/jpeg,image/png");
        result.MaxFileSizeBytes.Should().Be(3_000_000);
        result.AllowMultiple.Should().BeTrue();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var act = () => _sut.UpdateAsync(999, new UpdateOrgFileUploadConfigRequest
        {
            AllowedExtensions = ".pdf",
            AllowedMimeTypes  = "application/pdf",
            MaxFileSizeBytes  = 5_242_880,
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DelegatesToReadRepository()
    {
        var expected = new OrgFileUploadConfigResponse
        {
            Id    = 1,
            OrgId = 2,
            PageId = 3,
            AllowedExtensions = ".pdf",
            AllowedMimeTypes  = "application/pdf",
            MaxFileSizeBytes  = 5_242_880,
            IsActive          = true,
        };

        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
                It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.OrgId.Should().Be(2);
        result.AllowedExtensions.Should().Be(".pdf");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
                It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((OrgFileUploadConfigResponse?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── GetByScreen ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByScreenAsync_DelegatesToReadRepository()
    {
        var expected = new OrgFileUploadConfigResponse
        {
            Id     = 10,
            OrgId  = 5,
            PageId = 7,
            AllowedExtensions = ".docx",
            AllowedMimeTypes  = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            MaxFileSizeBytes  = 2_097_152,
            IsActive          = true,
        };

        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
                It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByScreenAsync(5, 7);

        result.Should().NotBeNull();
        result!.OrgId.Should().Be(5);
        result.PageId.Should().Be(7);
        result.AllowedExtensions.Should().Be(".docx");
    }

    [Fact]
    public async Task GetByScreenAsync_NotFound_ReturnsNull()
    {
        _readRepoMock
            .Setup(r => r.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
                It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync((OrgFileUploadConfigResponse?)null);

        var result = await _sut.GetByScreenAsync(99, 88);

        result.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<OrgFileUploadConfig> SeedConfigAsync(
        int    orgId        = 1,
        int    pageId       = 1,
        string extensions   = ".pdf",
        string mimeTypes    = "application/pdf",
        long   maxBytes     = 5_242_880,
        bool   allowMultiple = false)
    {
        var config = new OrgFileUploadConfig
        {
            OrgId             = orgId,
            PageId            = pageId,
            AllowedExtensions = extensions,
            AllowedMimeTypes  = mimeTypes,
            MaxFileSizeBytes  = maxBytes,
            AllowMultiple     = allowMultiple,
            IsActive          = true,
        };
        await _context.OrgFileUploadConfigs.AddAsync(config);
        await _context.SaveChangesAsync();
        return config;
    }
}
