using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Helpers;
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
            EntityId   = entityId,
            DateFrom   = pagination.DateFrom,
            DateTo     = pagination.DateTo,
            pagination.PageSize,
            Offset     = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            AuditLogQueries.GetByEntity,
            pagination.SortBy, pagination.SortDescending,
            AuditLogQueries.AllowedSortColumns, AuditLogQueries.DefaultSortColumn,
            defaultSortDescending: true);

        return await _readRepo.QueryPagedAsync<AuditLog>(
            dataSql, AuditLogQueries.CountByEntity, param, pagination.Page, pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByUserAsync(
        string userId, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            UserId   = userId,
            DateFrom = pagination.DateFrom,
            DateTo   = pagination.DateTo,
            pagination.PageSize,
            Offset   = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            AuditLogQueries.GetByUser,
            pagination.SortBy, pagination.SortDescending,
            AuditLogQueries.AllowedSortColumns, AuditLogQueries.DefaultSortColumn,
            defaultSortDescending: true);

        return await _readRepo.QueryPagedAsync<AuditLog>(
            dataSql, AuditLogQueries.CountByUser, param, pagination.Page, pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByScreenAsync(
        string screenName, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            ScreenName = screenName,
            DateFrom   = pagination.DateFrom,
            DateTo     = pagination.DateTo,
            pagination.PageSize,
            Offset     = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            AuditLogQueries.GetByScreen,
            pagination.SortBy, pagination.SortDescending,
            AuditLogQueries.AllowedSortColumns, AuditLogQueries.DefaultSortColumn,
            defaultSortDescending: true);

        return await _readRepo.QueryPagedAsync<AuditLog>(
            dataSql, AuditLogQueries.CountByScreen, param, pagination.Page, pagination.PageSize);
    }

    public async Task<PagedResult<AuditLog>> GetByTableAsync(
        string tableName, PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            TableName = tableName,
            DateFrom  = pagination.DateFrom,
            DateTo    = pagination.DateTo,
            pagination.PageSize,
            Offset    = pagination.Offset,
        };

        var dataSql = QueryBuilder.AppendPaging(
            AuditLogQueries.GetByTable,
            pagination.SortBy, pagination.SortDescending,
            AuditLogQueries.AllowedSortColumns, AuditLogQueries.DefaultSortColumn,
            defaultSortDescending: true);

        return await _readRepo.QueryPagedAsync<AuditLog>(
            dataSql, AuditLogQueries.CountByTable, param, pagination.Page, pagination.PageSize);
    }

    // ── Hierarchy ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<AuditLogBatchResponse>> GetByEntityHierarchyAsync(
        string entityName, string entityId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var entityParam = new { EntityName = entityName, EntityId = entityId };

        // Step 1: total number of distinct batches for this entity
        var total = await _readRepo.QueryFirstOrDefaultAsync<int>(
            AuditLogQueries.CountBatchesByEntity, entityParam);

        if (total == 0)
            return PagedResult<AuditLogBatchResponse>.Create(
                Enumerable.Empty<AuditLogBatchResponse>(), 0, pagination.Page, pagination.PageSize);

        // Step 2: paginated batch IDs ordered by most-recent first
        var batchParam = new
        {
            EntityName = entityName,
            EntityId   = entityId,
            pagination.PageSize,
            Offset     = pagination.Offset,
        };

        var batchRows = (await _readRepo.QueryAsync<BatchRow>(
            AuditLogQueries.GetBatchIdsByEntity, batchParam)).ToList();

        if (batchRows.Count == 0)
            return PagedResult<AuditLogBatchResponse>.Create(
                Enumerable.Empty<AuditLogBatchResponse>(), total, pagination.Page, pagination.PageSize);

        // Step 3: fetch ALL audit rows for those batches in one query
        var batchIds = batchRows.Select(b => b.BatchId).ToList();

        var allLogs = (await _readRepo.QueryAsync<AuditLog>(
            AuditLogQueries.GetAllByBatchIds, new { BatchIds = batchIds })).ToList();

        // Step 4: group by BatchId, build one hierarchy tree per batch
        var logsByBatch = allLogs
            .GroupBy(l => l.BatchId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());

        var batches = batchRows.Select(br =>
        {
            var logsInBatch = logsByBatch.TryGetValue(br.BatchId, out var group)
                ? group
                : new List<AuditLog>();

            // Use the audit row for the requested entity as the context source
            var contextEntry = logsInBatch.FirstOrDefault(l =>
                                   string.Equals(l.EntityName, entityName, StringComparison.OrdinalIgnoreCase)
                                   && l.EntityId == entityId)
                               ?? logsInBatch.FirstOrDefault();

            return new AuditLogBatchResponse
            {
                BatchId    = br.BatchId,
                Timestamp  = br.BatchTimestamp,
                ScreenName = contextEntry?.ScreenName,
                CreatedBy  = contextEntry?.CreatedBy,
                ModifiedBy = contextEntry?.ModifiedBy,
                IpAddress  = contextEntry?.IpAddress,
                Location   = contextEntry?.Location,
                Entries    = BuildHierarchy(logsInBatch),
            };
        }).ToList();

        return PagedResult<AuditLogBatchResponse>.Create(
            batches, total, pagination.Page, pagination.PageSize);
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a parent-child tree from a flat list of audit logs using ParentAuditLogId.
    /// Returns only the root nodes; each root's Children are populated recursively.
    /// </summary>
    private static IList<AuditLogNodeResponse> BuildHierarchy(IList<AuditLog> logs)
    {
        // Map every Id → its response node
        var nodeMap = logs.ToDictionary(
            l => l.Id,
            l => new AuditLogNodeResponse
            {
                Id         = l.Id,
                EntityName = l.EntityName,
                EntityId   = l.EntityId,
                Action     = l.Action,
                OldData    = l.OldData,
                NewData    = l.NewData,
                ModifiedBy = l.ModifiedBy,
                CreatedBy  = l.CreatedBy,
                ScreenName = l.ScreenName,
                TableName  = l.TableName,
                Timestamp  = l.Timestamp,
                IpAddress  = l.IpAddress,
                Location   = l.Location,
            });

        var roots = new List<AuditLogNodeResponse>();

        foreach (var log in logs)
        {
            var node = nodeMap[log.Id];

            if (log.ParentAuditLogId.HasValue
                && nodeMap.TryGetValue(log.ParentAuditLogId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    // ── private DTOs ──────────────────────────────────────────────────────────

    /// <summary>Internal Dapper projection for the batch-ID pagination query.</summary>
    private sealed class BatchRow
    {
        public string   BatchId        { get; set; } = string.Empty;
        public DateTime BatchTimestamp { get; set; }
    }
}
