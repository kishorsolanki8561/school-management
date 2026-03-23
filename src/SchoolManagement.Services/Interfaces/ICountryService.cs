using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface ICountryService
{
    Task<CountryResponse> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryResponse> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CountryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<CountryResponse>> GetAllAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);
}
