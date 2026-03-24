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
          AND  (@MenuId IS NULL OR MenuId = @MenuId)
          AND  (@Search  IS NULL OR Name LIKE '%' + @Search + '%')
        ORDER  BY Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAllPages = @"
        SELECT COUNT(*)
        FROM   PageMasters
        WHERE  IsDeleted = 0
          AND  (@MenuId IS NULL OR MenuId = @MenuId)
          AND  (@Search  IS NULL OR Name LIKE '%' + @Search + '%')";
}
