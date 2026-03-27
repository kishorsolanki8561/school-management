using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface IOrgFileUploadConfigService
{
    Task<OrgFileUploadConfigResponse>  CreateAsync   (CreateOrgFileUploadConfigRequest request,                    CancellationToken ct = default);
    Task<OrgFileUploadConfigResponse>  UpdateAsync   (int id, UpdateOrgFileUploadConfigRequest request,            CancellationToken ct = default);
    Task<OrgFileUploadConfigResponse?> GetByIdAsync  (int id,                                                      CancellationToken ct = default);
    Task<OrgFileUploadConfigResponse?> GetByScreenAsync(int orgId, int pageId,                                     CancellationToken ct = default);
}
