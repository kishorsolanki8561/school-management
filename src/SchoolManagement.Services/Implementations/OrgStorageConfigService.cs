using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class OrgStorageConfigService : IOrgStorageConfigService
{
    private readonly SchoolManagementDbContext _db;
    private readonly IRequestContext           _requestContext;

    public OrgStorageConfigService(SchoolManagementDbContext db, IRequestContext requestContext)
    {
        _db             = db;
        _requestContext = requestContext;
    }

    public async Task<OrgStorageConfigResponse> SaveAsync(SaveOrgStorageConfigRequest request, CancellationToken ct = default)
    {
        var orgId = ResolveOrgId();

        var existing = await _db.OrgStorageConfigs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.OrgId == orgId, ct);

        if (existing is null)
        {
            existing = new OrgStorageConfig { OrgId = orgId };
            _db.OrgStorageConfigs.Add(existing);
        }
        else if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
        }

        existing.StorageType     = request.StorageType;
        existing.IsActive        = true;
        existing.BasePath        = request.BasePath;
        existing.BucketName      = request.BucketName;
        existing.Region          = request.Region;
        existing.AccessKey       = request.AccessKey;
        existing.SecretKey       = request.SecretKey;
        existing.ContainerName   = request.ContainerName;
        existing.ConnectionString = request.ConnectionString;

        await _db.SaveChangesAsync(ct);
        return ToResponse(existing);
    }

    public async Task<OrgStorageConfigResponse?> GetByOrgIdAsync(int orgId, CancellationToken ct = default)
    {
        var entity = await _db.OrgStorageConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrgId == orgId, ct);

        return entity is null ? null : ToResponse(entity);
    }

    public async Task DeleteAsync(int orgId, CancellationToken ct = default)
    {
        var entity = await _db.OrgStorageConfigs
            .FirstOrDefaultAsync(x => x.OrgId == orgId, ct);

        if (entity is null) return;

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private int ResolveOrgId()
    {
        if (_requestContext.OrgId is null)
            throw new InvalidOperationException("OrgId is required to manage storage config.");
        return _requestContext.OrgId.Value;
    }

    private static OrgStorageConfigResponse ToResponse(OrgStorageConfig e) => new(
        e.Id,
        e.OrgId,
        e.StorageType,
        e.IsActive,
        e.BasePath,
        e.BucketName,
        e.Region,
        e.AccessKey,
        e.ContainerName
    );
}
