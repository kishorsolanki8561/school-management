using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class FileUploadService : IFileUploadService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IFilesValidator           _filesValidator;
    private readonly IFilePathHelper           _filePathHelper;
    private readonly IRequestContext           _requestContext;
    private readonly FileUploadDefaults        _defaults;

    public FileUploadService(
        SchoolManagementDbContext context,
        IFilesValidator           filesValidator,
        IFilePathHelper           filePathHelper,
        IRequestContext           requestContext)
    {
        _context        = context;
        _filesValidator = filesValidator;
        _filePathHelper = filePathHelper;
        _requestContext = requestContext;
        _defaults       = InitializeConfiguration.FileUploadDefaults;
    }

    public async Task<IList<FileUploadResponse>> UploadAsync(
        IList<IFormFile> files,
        int?             pageId,
        int?             orgId,
        CancellationToken ct = default)
    {
        var config    = await ResolveConfigAsync(pageId, orgId, ct);
        var uploadDir = _filePathHelper.GetUploadPath(await ResolveFolderAsync(orgId, pageId, ct));

        if (!config.AllowMultiple && files.Count > 1)
            throw new InvalidOperationException(AppMessages.OrgFileUploadConfig.MultipleFilesNotAllowed);

        var responses = new List<FileUploadResponse>(files.Count);

        foreach (var file in files)
        {
            var result = _filesValidator.Validate(
                file.FileName,
                file.Length,
                file.ContentType,
                config.AllowedExtensions,
                config.AllowedMimeTypes,
                config.MaxFileSizeBytes);

            if (!result.IsValid)
                throw new ArgumentException(string.Join("; ", result.Errors));

            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath   = Path.Combine(uploadDir, uniqueName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream, ct);

            responses.Add(new FileUploadResponse
            {
                FileName    = file.FileName,
                FilePath    = fullPath,
                SizeBytes   = file.Length,
                ContentType = file.ContentType,
            });
        }

        return responses;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private sealed record ResolvedConfig(
        IEnumerable<string> AllowedExtensions,
        IEnumerable<string> AllowedMimeTypes,
        long                MaxFileSizeBytes,
        bool                AllowMultiple);

    private async Task<ResolvedConfig> ResolveConfigAsync(
        int? pageId, int? orgId, CancellationToken ct)
    {
        // OwnerAdmin always uses appsettings defaults
        if (_requestContext.Role == nameof(UserRole.OwnerAdmin))
            return FromDefaults();

        // Need both ids to resolve a screen-specific config
        if (orgId is null || pageId is null)
            return FromDefaults();

        var cfg = await _context.OrgFileUploadConfigs
            .FirstOrDefaultAsync(c => c.OrgId == orgId.Value && c.PageId == pageId.Value && c.IsActive, ct);

        if (cfg is not null)
            return new ResolvedConfig(
                AllowedExtensions: cfg.AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                AllowedMimeTypes:  cfg.AllowedMimeTypes.Split(',',  StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                MaxFileSizeBytes:  cfg.MaxFileSizeBytes,
                AllowMultiple:     cfg.AllowMultiple);

        // Fallback to appsettings defaults
        return FromDefaults();
    }

    /// <summary>
    /// Resolves the upload subfolder name based on org/page names.
    /// Priority: {OrgName}/{PageName} → {PageName} → {OrgName} → "AllAttachment"
    /// </summary>
    private async Task<string> ResolveFolderAsync(int? orgId, int? pageId, CancellationToken ct)
    {
        if (orgId is null && pageId is null)
            return "AllAttachment";

        string? orgName  = null;
        string? pageName = null;

        if (orgId.HasValue)
        {
            var org = await _context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orgId.Value, ct);
            orgName = org is not null ? SanitizeName(org.Name) : null;
        }

        if (pageId.HasValue)
        {
            var page = await _context.PageMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pageId.Value, ct);
            pageName = page is not null ? SanitizeName(page.Name) : null;
        }

        if (orgName is not null && pageName is not null)
            return Path.Combine(orgName, pageName);

        return pageName ?? orgName ?? "AllAttachment";
    }

    /// <summary>Replaces characters that are illegal in directory names with underscores.</summary>
    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    }

    private ResolvedConfig FromDefaults() =>
        new(_defaults.AllowedExtensions, _defaults.AllowedMimeTypes,
            _defaults.MaxFileSizeBytes,  _defaults.AllowMultiple);
}
