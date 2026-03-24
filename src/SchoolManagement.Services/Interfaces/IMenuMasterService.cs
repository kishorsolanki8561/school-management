using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;

namespace SchoolManagement.Services.Interfaces;

public interface IMenuMasterService
{
    Task<MenuResponse>             CreateAsync (CreateMenuRequest request,       CancellationToken cancellationToken = default);
    Task<MenuResponse>             UpdateAsync (int id, UpdateMenuRequest request, CancellationToken cancellationToken = default);
    Task                           DeleteAsync (int id,                           CancellationToken cancellationToken = default);
    Task<MenuResponse?>            GetByIdAsync(int id,                           CancellationToken cancellationToken = default);
    Task<PagedResult<MenuResponse>> GetAllAsync (PaginationRequest request,        CancellationToken cancellationToken = default);
}
