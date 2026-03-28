using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLog>> GetByEntityAsync(string entityName, string entityId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetByUserAsync(string userId, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetByScreenAsync(string screenName, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLog>> GetByTableAsync(string tableName, PaginationRequest pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns paginated batches for a given entity, each batch containing a full parent-child
    /// hierarchy tree of every audit log row saved in the same DB transaction (BatchId).
    /// </summary>
    Task<PagedResult<AuditLogBatchResponse>> GetByEntityHierarchyAsync(string entityName, string entityId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
