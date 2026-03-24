namespace SchoolManagement.Services.Constants;

internal static class MenuAndPagePermissionQueries
{
    public const string GetById = @"
        SELECT Id, MenuId, PageId, PageModuleId, ActionId, RoleId, IsAllowed, CreatedAt
        FROM   MenuAndPagePermissions
        WHERE  Id        = @Id
          AND  IsDeleted = 0";

    public const string GetAll = @"
        SELECT Id, MenuId, PageId, PageModuleId, ActionId, RoleId, IsAllowed, CreatedAt
        FROM   MenuAndPagePermissions
        WHERE  IsDeleted = 0
          AND  (@MenuId IS NULL OR MenuId = @MenuId)
          AND  (@PageId IS NULL OR PageId = @PageId)
          AND  (@RoleId IS NULL OR RoleId = @RoleId)
        ORDER  BY MenuId, PageId, PageModuleId, ActionId, RoleId
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   MenuAndPagePermissions
        WHERE  IsDeleted = 0
          AND  (@MenuId IS NULL OR MenuId = @MenuId)
          AND  (@PageId IS NULL OR PageId = @PageId)
          AND  (@RoleId IS NULL OR RoleId = @RoleId)";
}
