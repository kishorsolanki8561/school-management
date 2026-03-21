using SchoolManagement.Models.Common;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLog>> GetByEntityAsync(string entityName, string entityId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetByUserAsync(string userId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
