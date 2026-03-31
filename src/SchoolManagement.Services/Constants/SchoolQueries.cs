namespace SchoolManagement.Services.Constants;

internal static class SchoolQueries
{
    public const string GetById = @"
        SELECT Id, Name, Address, SchoolCode, IsActive, IsApproved, ApprovedAt, ApprovedBy, CreatedAt
        FROM   Organizations
        WHERE  Id = @Id AND IsDeleted = 0";

    public const string GetAll = @"
        SELECT Id, Name, Address, SchoolCode, IsActive, IsApproved, ApprovedAt, ApprovedBy, CreatedAt
        FROM   Organizations
        WHERE  IsDeleted = 0
          AND  (@Search     IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')
          AND  (@IsActive   IS NULL OR IsActive   = @IsActive)
          AND  (@IsApproved IS NULL OR IsApproved = @IsApproved)
          AND  (@DateFrom   IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo     IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   Organizations
        WHERE  IsDeleted = 0
          AND  (@Search     IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')
          AND  (@IsActive   IS NULL OR IsActive   = @IsActive)
          AND  (@IsApproved IS NULL OR IsApproved = @IsApproved)
          AND  (@DateFrom   IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo     IS NULL OR CreatedAt <= @DateTo)";

    public const string GetPendingApprovals = @"
        SELECT sar.Id, sar.OrgId, o.Name AS OrgName,
               sar.RequestedByUserId, ru.Username AS RequestedByUsername,
               sar.Status, sar.RejectionReason,
               rv.Username AS ReviewedByUsername, sar.ReviewedAt, sar.CreatedAt
        FROM   SchoolApprovalRequests sar
        INNER JOIN Organizations o  ON o.Id  = sar.OrgId             AND o.IsDeleted  = 0
        INNER JOIN Users ru         ON ru.Id = sar.RequestedByUserId  AND ru.IsDeleted = 0
        LEFT  JOIN Users rv         ON rv.Id = sar.ReviewedByUserId   AND rv.IsDeleted = 0
        WHERE  sar.IsDeleted = 0 AND sar.Status = 1
        ORDER  BY sar.CreatedAt ASC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountPendingApprovals = @"
        SELECT COUNT(*)
        FROM   SchoolApprovalRequests
        WHERE  IsDeleted = 0 AND Status = 1";

    public const string GetApprovalHistory = @"
        SELECT sar.Id, sar.OrgId, o.Name AS OrgName,
               sar.RequestedByUserId, ru.Username AS RequestedByUsername,
               sar.Status, sar.RejectionReason,
               rv.Username AS ReviewedByUsername, sar.ReviewedAt, sar.CreatedAt
        FROM   SchoolApprovalRequests sar
        INNER JOIN Organizations o  ON o.Id  = sar.OrgId             AND o.IsDeleted  = 0
        INNER JOIN Users ru         ON ru.Id = sar.RequestedByUserId  AND ru.IsDeleted = 0
        LEFT  JOIN Users rv         ON rv.Id = sar.ReviewedByUserId   AND rv.IsDeleted = 0
        WHERE  sar.IsDeleted = 0 AND sar.OrgId = @OrgId
        ORDER  BY sar.CreatedAt DESC";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "IsActive", "IsApproved", "CreatedAt" };
    public const string DefaultSortColumn = "Name";
}
