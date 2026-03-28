namespace SchoolManagement.Services.Constants;

internal static class PageMasterQueries
{
    public const string GetPageById = @"
        SELECT Id, Name, IconClass, PageUrl, MenuId, IsActive, IsUsePageForOwnerAdmin, CreatedAt
        FROM   PageMasters
        WHERE  Id        = @Id
          AND  IsDeleted = 0";

    public const string GetAllPages = @"
        SELECT Id, Name, IconClass, PageUrl, MenuId, IsActive, IsUsePageForOwnerAdmin, CreatedAt
        FROM   PageMasters
        WHERE  IsDeleted = 0
          AND  (@MenuId   IS NULL OR MenuId   = @MenuId)
          AND  (@Search   IS NULL OR Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAllPages = @"
        SELECT COUNT(*)
        FROM   PageMasters
        WHERE  IsDeleted = 0
          AND  (@MenuId   IS NULL OR MenuId   = @MenuId)
          AND  (@Search   IS NULL OR Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "PageUrl", "IsActive", "CreatedAt", "MenuId" };
    public const string DefaultSortColumn = "Name";
}
