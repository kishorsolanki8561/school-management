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
}
