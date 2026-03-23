using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface ICityService
{
    Task<CityResponse> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default);
    Task<CityResponse> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CityResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<CityResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<CityResponse>> GetByStateAsync(int stateId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
