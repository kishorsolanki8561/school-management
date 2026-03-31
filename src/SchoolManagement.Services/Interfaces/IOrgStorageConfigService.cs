using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface IOrgStorageConfigService
{
    /// <summary>Upserts the org storage config. OrgId is taken from IRequestContext.</summary>
    Task<OrgStorageConfigResponse> SaveAsync(SaveOrgStorageConfigRequest request, CancellationToken ct = default);

    Task<OrgStorageConfigResponse?> GetByOrgIdAsync(int orgId, CancellationToken ct = default);

    Task DeleteAsync(int orgId, CancellationToken ct = default);
}
