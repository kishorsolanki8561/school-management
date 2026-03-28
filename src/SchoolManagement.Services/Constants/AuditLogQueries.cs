namespace SchoolManagement.Services.Constants;

internal static class AuditLogQueries
{
    public const string GetByEntity = @"
        SELECT * FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId
          AND (@DateFrom IS NULL OR Timestamp >= @DateFrom)
          AND (@DateTo   IS NULL OR Timestamp <= @DateTo)";

    public const string CountByEntity = @"
        SELECT COUNT(*) FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId
          AND (@DateFrom IS NULL OR Timestamp >= @DateFrom)
          AND (@DateTo   IS NULL OR Timestamp <= @DateTo)";

    public const string GetByUser = @"
        SELECT * FROM AuditLogs
        WHERE CreatedBy = @UserId
          AND (@DateFrom IS NULL OR Timestamp >= @DateFrom)
          AND (@DateTo   IS NULL OR Timestamp <= @DateTo)";

    public const string CountByUser =
        "SELECT COUNT(*) FROM AuditLogs WHERE CreatedBy = @UserId AND (@DateFrom IS NULL OR Timestamp >= @DateFrom) AND (@DateTo IS NULL OR Timestamp <= @DateTo)";

    public const string GetByScreen = @"
        SELECT * FROM AuditLogs
        WHERE ScreenName = @ScreenName
          AND (@DateFrom IS NULL OR Timestamp >= @DateFrom)
          AND (@DateTo   IS NULL OR Timestamp <= @DateTo)";

    public const string CountByScreen =
        "SELECT COUNT(*) FROM AuditLogs WHERE ScreenName = @ScreenName AND (@DateFrom IS NULL OR Timestamp >= @DateFrom) AND (@DateTo IS NULL OR Timestamp <= @DateTo)";

    public const string GetByTable = @"
        SELECT * FROM AuditLogs
        WHERE TableName = @TableName
          AND (@DateFrom IS NULL OR Timestamp >= @DateFrom)
          AND (@DateTo   IS NULL OR Timestamp <= @DateTo)";

    public const string CountByTable =
        "SELECT COUNT(*) FROM AuditLogs WHERE TableName = @TableName AND (@DateFrom IS NULL OR Timestamp >= @DateFrom) AND (@DateTo IS NULL OR Timestamp <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Timestamp", "Action", "EntityName", "TableName", "ScreenName", "CreatedBy" };
    public const string DefaultSortColumn = "Timestamp";

    // ── Hierarchy (batch-grouped) queries ─────────────────────────────────────

    /// <summary>
    /// Returns one row per distinct BatchId that contains an entry for the requested entity,
    /// ordered by the earliest timestamp in the batch (most-recent first).
    /// Used to drive paging — each "page" is N batches.
    /// </summary>
    public const string GetBatchIdsByEntity = @"
        SELECT BatchId, MIN(Timestamp) AS BatchTimestamp
        FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId
        GROUP BY BatchId
        ORDER BY BatchTimestamp DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    /// <summary>Total number of distinct batches that touched the requested entity.</summary>
    public const string CountBatchesByEntity = @"
        SELECT COUNT(DISTINCT BatchId)
        FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId";

    /// <summary>
    /// Fetches every audit log row that belongs to the given set of BatchIds.
    /// Returns ordered by Timestamp ASC, Id ASC so parent rows always precede children
    /// when the caller builds the hierarchy tree.
    /// </summary>
    public const string GetAllByBatchIds = @"
        SELECT * FROM AuditLogs
        WHERE BatchId IN @BatchIds
        ORDER BY Timestamp ASC, Id ASC";
}
