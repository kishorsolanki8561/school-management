using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class FileUploadServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IFilesValidator>     _validatorMock  = new();
    private readonly Mock<IFilePathHelper>     _filePathMock   = new();
    private readonly Mock<IRequestContext>     _requestCtxMock = new();
    private readonly FileUploadService         _sut;

    private static readonly string[] DefaultExtensions = new[] { ".pdf", ".jpg" };
    private static readonly string[] DefaultMimeTypes  = new[] { "application/pdf", "image/jpeg" };
    private const long DefaultMaxBytes = 5_242_880;

    public FileUploadServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);

        // Seed static FileUploadDefaults used by FileUploadService
        InitializeConfiguration.FileUploadDefaults.AllowedExtensions = DefaultExtensions;
        InitializeConfiguration.FileUploadDefaults.AllowedMimeTypes  = DefaultMimeTypes;
        InitializeConfiguration.FileUploadDefaults.MaxFileSizeBytes  = DefaultMaxBytes;
        InitializeConfiguration.FileUploadDefaults.AllowMultiple     = false;

        // Point all uploads to the OS temp folder (always exists — no disk cleanup needed)
        _filePathMock
            .Setup(h => h.GetUploadPath(It.IsAny<string>()))
            .Returns(Path.GetTempPath());

        _sut = new FileUploadService(
            _context,
            _validatorMock.Object,
            _filePathMock.Object,
            _requestCtxMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── OwnerAdmin → always uses appsettings defaults ─────────────────────────

    [Fact]
    public async Task UploadAsync_OwnerAdmin_UsesAppsettingsDefaults()
    {
        SetRole("OwnerAdmin");

        // Seed an org config that should be ignored because caller is OwnerAdmin
        await SeedOrgConfigAsync(orgId: 1, pageId: 1,
            extensions: ".png", mimeTypes: "image/png", maxBytes: 100_000, allowMultiple: false);

        var file = BuildFileMock("report.pdf", 1024, "application/pdf");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { file.Object }, pageId: 1, orgId: 1);

        // Validator must receive the appsettings defaults, NOT the org config values
        _validatorMock.Verify(v => v.Validate(
            "report.pdf", 1024, "application/pdf",
            It.Is<IEnumerable<string>>(e => e.SequenceEqual(DefaultExtensions)),
            It.Is<IEnumerable<string>>(m => m.SequenceEqual(DefaultMimeTypes)),
            DefaultMaxBytes), Times.Once);
    }

    // ── Non-admin with org config → uses org config ───────────────────────────

    [Fact]
    public async Task UploadAsync_NonAdminWithOrgConfig_UsesOrgConfig()
    {
        SetRole("SchoolAdmin");
        await SeedOrgConfigAsync(orgId: 2, pageId: 3,
            extensions: ".png", mimeTypes: "image/png", maxBytes: 1_000_000, allowMultiple: false);

        var file = BuildFileMock("photo.png", 500_000, "image/png");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { file.Object }, pageId: 3, orgId: 2);

        _validatorMock.Verify(v => v.Validate(
            "photo.png", 500_000, "image/png",
            It.Is<IEnumerable<string>>(e => e.Contains(".png")),
            It.Is<IEnumerable<string>>(m => m.Contains("image/png")),
            1_000_000), Times.Once);
    }

    // ── Non-admin without org config → falls back to appsettings ─────────────

    [Fact]
    public async Task UploadAsync_NonAdminNoOrgConfig_FallsBackToDefaults()
    {
        SetRole("SchoolAdmin");
        // No org config seeded for orgId=50, pageId=60

        var file = BuildFileMock("doc.pdf", 2048, "application/pdf");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        var result = await _sut.UploadAsync(new[] { file.Object }, pageId: 60, orgId: 50);

        result.Should().HaveCount(1);
        _validatorMock.Verify(v => v.Validate(
            "doc.pdf", 2048, "application/pdf",
            It.Is<IEnumerable<string>>(e => e.SequenceEqual(DefaultExtensions)),
            It.Is<IEnumerable<string>>(m => m.SequenceEqual(DefaultMimeTypes)),
            DefaultMaxBytes), Times.Once);
    }

    // ── AllowMultiple=false + 2 files → InvalidOperationException ────────────

    [Fact]
    public async Task UploadAsync_MultipleFilesWhenNotAllowed_ThrowsInvalidOperationException()
    {
        SetRole("SchoolAdmin");
        await SeedOrgConfigAsync(orgId: 4, pageId: 5,
            extensions: ".pdf", mimeTypes: "application/pdf",
            maxBytes: 5_242_880, allowMultiple: false);

        var file1 = BuildFileMock("a.pdf", 1024, "application/pdf");
        var file2 = BuildFileMock("b.pdf", 2048, "application/pdf");

        var act = () => _sut.UploadAsync(new[] { file1.Object, file2.Object }, pageId: 5, orgId: 4);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── OwnerAdmin AllowMultiple=false by default + 2 files → throws ──────────

    [Fact]
    public async Task UploadAsync_OwnerAdmin_DefaultAllowMultipleFalse_TwoFiles_Throws()
    {
        SetRole("OwnerAdmin");
        // Defaults have AllowMultiple=false (set in constructor)

        var file1 = BuildFileMock("a.pdf", 1024, "application/pdf");
        var file2 = BuildFileMock("b.pdf", 2048, "application/pdf");

        var act = () => _sut.UploadAsync(new[] { file1.Object, file2.Object }, pageId: 1, orgId: 1);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Invalid extension → ArgumentException ────────────────────────────────

    [Fact]
    public async Task UploadAsync_InvalidExtension_ThrowsArgumentException()
    {
        SetRole("OwnerAdmin");

        var file = BuildFileMock("virus.exe", 1024, "application/x-msdownload");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Failure(new[] { "File extension '.exe' is not allowed." }));

        var act = () => _sut.UploadAsync(new[] { file.Object }, pageId: 1, orgId: 1);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*extension*");
    }

    // ── File too large → ArgumentException ───────────────────────────────────

    [Fact]
    public async Task UploadAsync_FileTooLarge_ThrowsArgumentException()
    {
        SetRole("OwnerAdmin");

        var file = BuildFileMock("huge.pdf", 100_000_000, "application/pdf");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Failure(new[] { "File size 100000000 bytes exceeds maximum." }));

        var act = () => _sut.UploadAsync(new[] { file.Object }, pageId: 1, orgId: 1);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*size*");
    }

    // ── Valid single file → correct FileUploadResponse ───────────────────────

    [Fact]
    public async Task UploadAsync_ValidSingleFile_ReturnsCorrectResponse()
    {
        SetRole("OwnerAdmin");

        var file = BuildFileMock("invoice.pdf", 4096, "application/pdf");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        var result = await _sut.UploadAsync(new[] { file.Object }, pageId: 5, orgId: 7);

        result.Should().HaveCount(1);
        result[0].FileName.Should().Be("invoice.pdf");
        result[0].SizeBytes.Should().Be(4096);
        result[0].ContentType.Should().Be("application/pdf");
        result[0].FilePath.Should().StartWith(Path.GetTempPath());
        result[0].FilePath.Should().EndWith(".pdf");
    }

    // ── AllowMultiple=true → all files returned ───────────────────────────────

    [Fact]
    public async Task UploadAsync_AllowMultipleTrue_ReturnsAllFiles()
    {
        SetRole("SchoolAdmin");
        await SeedOrgConfigAsync(orgId: 7, pageId: 8,
            extensions: ".pdf,.jpg", mimeTypes: "application/pdf,image/jpeg",
            maxBytes: 5_242_880, allowMultiple: true);

        var file1 = BuildFileMock("a.pdf", 1024, "application/pdf");
        var file2 = BuildFileMock("b.jpg", 2048, "image/jpeg");
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        var result = await _sut.UploadAsync(new[] { file1.Object, file2.Object }, pageId: 8, orgId: 7);

        result.Should().HaveCount(2);
        result[0].FileName.Should().Be("a.pdf");
        result[1].FileName.Should().Be("b.jpg");
    }

    // ── Folder resolution ─────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_BothOrgAndPageProvided_FolderIsOrgNameSlashPageName()
    {
        SetRole("OwnerAdmin");
        await SeedOrgAsync(id: 10, name: "Sunrise Academy");
        await SeedPageAsync(id: 20, name: "Student Documents");

        string? capturedSubfolder = null;
        _filePathMock
            .Setup(h => h.GetUploadPath(It.IsAny<string>()))
            .Callback<string>(s => capturedSubfolder = s)
            .Returns(Path.GetTempPath());

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { BuildFileMock("f.pdf", 1024, "application/pdf").Object },
            pageId: 20, orgId: 10);

        capturedSubfolder.Should().Be(Path.Combine("Sunrise Academy", "Student Documents"));
    }

    [Fact]
    public async Task UploadAsync_NullOrgId_FolderIsPageNameOnly()
    {
        SetRole("OwnerAdmin");
        await SeedPageAsync(id: 30, name: "Admissions");

        string? capturedSubfolder = null;
        _filePathMock
            .Setup(h => h.GetUploadPath(It.IsAny<string>()))
            .Callback<string>(s => capturedSubfolder = s)
            .Returns(Path.GetTempPath());

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { BuildFileMock("f.pdf", 1024, "application/pdf").Object },
            pageId: 30, orgId: null);

        capturedSubfolder.Should().Be("Admissions");
    }

    [Fact]
    public async Task UploadAsync_NullPageId_FolderIsOrgNameOnly()
    {
        SetRole("OwnerAdmin");
        await SeedOrgAsync(id: 40, name: "Greenfield School");

        string? capturedSubfolder = null;
        _filePathMock
            .Setup(h => h.GetUploadPath(It.IsAny<string>()))
            .Callback<string>(s => capturedSubfolder = s)
            .Returns(Path.GetTempPath());

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { BuildFileMock("f.pdf", 1024, "application/pdf").Object },
            pageId: null, orgId: 40);

        capturedSubfolder.Should().Be("Greenfield School");
    }

    [Fact]
    public async Task UploadAsync_BothNull_FolderIsAllAttachment()
    {
        SetRole("OwnerAdmin");

        string? capturedSubfolder = null;
        _filePathMock
            .Setup(h => h.GetUploadPath(It.IsAny<string>()))
            .Callback<string>(s => capturedSubfolder = s)
            .Returns(Path.GetTempPath());

        _validatorMock
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<long>()))
            .Returns(ValidationResult.Success());

        await _sut.UploadAsync(new[] { BuildFileMock("f.pdf", 1024, "application/pdf").Object },
            pageId: null, orgId: null);

        capturedSubfolder.Should().Be("AllAttachment");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetRole(string role) =>
        _requestCtxMock.Setup(r => r.Role).Returns(role);

    private static Mock<IFormFile> BuildFileMock(string fileName, long size, string contentType)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private async Task SeedOrgAsync(int id, string name)
    {
        var org = new Organization { Id = id, Name = name, IsActive = true };
        await _context.Organizations.AddAsync(org);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task SeedPageAsync(int id, string name)
    {
        // PageMaster requires a MenuMaster FK; seed a stub menu first if needed
        if (!_context.MenuMasters.Any(m => m.Id == 1))
        {
            await _context.MenuMasters.AddAsync(new MenuMaster { Id = 1, Name = "TestMenu" });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        var page = new PageMaster { Id = id, Name = name, PageUrl = $"/test/{id}", MenuId = 1 };
        await _context.PageMasters.AddAsync(page);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task SeedOrgConfigAsync(
        int    orgId,
        int    pageId,
        string extensions,
        string mimeTypes,
        long   maxBytes,
        bool   allowMultiple)
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
    }
}
