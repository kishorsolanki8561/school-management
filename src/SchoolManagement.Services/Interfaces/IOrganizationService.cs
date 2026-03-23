using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<OrganizationResponse> UpdateAsync(int id, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<OrganizationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<OrganizationResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
}
