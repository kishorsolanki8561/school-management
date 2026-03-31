using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface IUserManagementService
{
    Task<UserResponse>              CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserResponse>              UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task                            DeleteAsync(int id, CancellationToken ct = default);
    Task<UserResponse?>             GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<UserResponse>> GetAllAsync(PaginationRequest request, CancellationToken ct = default);

    Task<UserResponse>              AssignRoleAsync(int userId, AssignRoleRequest request, CancellationToken ct = default);
    Task<UserResponse>              RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default);

    Task<UserResponse>              ChangeRoleLevelAsync(int userId, ChangeRoleLevelRequest request, CancellationToken ct = default);
}
