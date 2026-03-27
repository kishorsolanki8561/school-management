using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class OrgFileUploadConfigService : IOrgFileUploadConfigService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IReadRepository          _readRepo;

    public OrgFileUploadConfigService(SchoolManagementDbContext context, IReadRepository readRepo)
    {
        _context  = context;
        _readRepo = readRepo;
    }

    public async Task<OrgFileUploadConfigResponse> CreateAsync(
        CreateOrgFileUploadConfigRequest request, CancellationToken ct = default)
    {
        var duplicate = await _context.OrgFileUploadConfigs
            .AnyAsync(c => c.OrgId == request.OrgId && c.PageId == request.PageId, ct);

        if (duplicate)
            throw new InvalidOperationException(
                AppMessages.OrgFileUploadConfig.AlreadyExists(request.OrgId, request.PageId));

        var config = new OrgFileUploadConfig
        {
            OrgId             = request.OrgId,
            PageId            = request.PageId,
            AllowedExtensions = request.AllowedExtensions,
            AllowedMimeTypes  = request.AllowedMimeTypes,
            MaxFileSizeBytes  = request.MaxFileSizeBytes,
            AllowMultiple     = request.AllowMultiple,
            IsActive          = true,
        };

        await _context.OrgFileUploadConfigs.AddAsync(config, ct);
        await _context.SaveChangesAsync(ct);

        return ToResponse(config);
    }

    public async Task<OrgFileUploadConfigResponse> UpdateAsync(
        int id, UpdateOrgFileUploadConfigRequest request, CancellationToken ct = default)
    {
        var config = await _context.OrgFileUploadConfigs.FindAsync(new object[] { id }, ct)
            ?? throw new KeyNotFoundException(AppMessages.OrgFileUploadConfig.NotFound(id));

        config.AllowedExtensions = request.AllowedExtensions;
        config.AllowedMimeTypes  = request.AllowedMimeTypes;
        config.MaxFileSizeBytes  = request.MaxFileSizeBytes;
        config.AllowMultiple     = request.AllowMultiple;
        config.IsActive          = request.IsActive;

        await _context.SaveChangesAsync(ct);

        return ToResponse(config);
    }

    public Task<OrgFileUploadConfigResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        => _readRepo.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
               OrgFileUploadConfigQueries.GetById, new { Id = id });

    public Task<OrgFileUploadConfigResponse?> GetByScreenAsync(
        int orgId, int pageId, CancellationToken ct = default)
        => _readRepo.QueryFirstOrDefaultAsync<OrgFileUploadConfigResponse>(
               OrgFileUploadConfigQueries.GetByScreen, new { OrgId = orgId, PageId = pageId });

    // ── Private helpers ───────────────────────────────────────────────────────

    private static OrgFileUploadConfigResponse ToResponse(OrgFileUploadConfig c) =>
        new()
        {
            Id                = c.Id,
            OrgId             = c.OrgId,
            PageId            = c.PageId,
            AllowedExtensions = c.AllowedExtensions,
            AllowedMimeTypes  = c.AllowedMimeTypes,
            MaxFileSizeBytes  = c.MaxFileSizeBytes,
            AllowMultiple     = c.AllowMultiple,
            IsActive          = c.IsActive,
            CreatedAt         = c.CreatedAt,
        };
}
