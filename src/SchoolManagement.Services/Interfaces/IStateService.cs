using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IStateService
{
    Task<StateResponse> CreateAsync(CreateStateRequest request, CancellationToken cancellationToken = default);
    Task<StateResponse> UpdateAsync(int id, UpdateStateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<StateResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<StateResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<StateResponse>> GetByCountryAsync(int countryId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
