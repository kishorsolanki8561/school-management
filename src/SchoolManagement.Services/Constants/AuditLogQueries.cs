namespace SchoolManagement.Services.Constants;

internal static class AuditLogQueries
{
    public const string GetByEntity = @"
        SELECT * FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId
        ORDER BY Timestamp DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByEntity = @"
        SELECT COUNT(*) FROM AuditLogs
        WHERE EntityName = @EntityName AND EntityId = @EntityId";

    public const string GetByUser = @"
        SELECT * FROM AuditLogs
        WHERE ModifiedBy = @UserId
        ORDER BY Timestamp DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByUser =
        "SELECT COUNT(*) FROM AuditLogs WHERE ModifiedBy = @UserId";

    public const string GetByScreen = @"
        SELECT * FROM AuditLogs
        WHERE ScreenName = @ScreenName
        ORDER BY Timestamp DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByScreen =
        "SELECT COUNT(*) FROM AuditLogs WHERE ScreenName = @ScreenName";

    public const string GetByTable = @"
        SELECT * FROM AuditLogs
        WHERE TableName = @TableName
        ORDER BY Timestamp DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByTable =
        "SELECT COUNT(*) FROM AuditLogs WHERE TableName = @TableName";
}
