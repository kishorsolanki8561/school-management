using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IReadRepository _readRepo;

    public AuditLogService(IReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<PagedResult<AuditLog>> GetByEntityAsync(
        string entityName, string entityId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            EntityName = entityName,
            EntityId = entityId,
            pagination.PageSize,
            Offset = pagination.Offset
        };

        return await _readRepo.QueryPagedAsync<AuditLog>(
            AuditLogQueries.GetByEntity,
            AuditLogQueries.CountByEntity,
            param,
            pagination.Page,
            pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByUserAsync(
        string userId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            UserId = userId,
            pagination.PageSize,
            Offset = pagination.Offset
        };

        return await _readRepo.QueryPagedAsync<AuditLog>(
            AuditLogQueries.GetByUser,
            AuditLogQueries.CountByUser,
            param,
            pagination.Page,
            pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByScreenAsync(
        string screenName, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            ScreenName = screenName,
            pagination.PageSize,
            Offset = pagination.Offset
        };

        return await _readRepo.QueryPagedAsync<AuditLog>(
            AuditLogQueries.GetByScreen,
            AuditLogQueries.CountByScreen,
            param,
            pagination.Page,
            pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByTableAsync(
        string tableName, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            TableName = tableName,
            pagination.PageSize,
            Offset = pagination.Offset
        };

        return await _readRepo.QueryPagedAsync<AuditLog>(
            AuditLogQueries.GetByTable,
            AuditLogQueries.CountByTable,
            param,
            pagination.Page,
            pagination.PageSize);
    }
}
