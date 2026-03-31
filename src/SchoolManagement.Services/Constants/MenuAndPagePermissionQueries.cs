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
          AND  (@MenuId   IS NULL OR MenuId = @MenuId)
          AND  (@PageId   IS NULL OR PageId = @PageId)
          AND  (@RoleId   IS NULL OR RoleId = @RoleId)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   MenuAndPagePermissions
        WHERE  IsDeleted = 0
          AND  (@MenuId   IS NULL OR MenuId = @MenuId)
          AND  (@PageId   IS NULL OR PageId = @PageId)
          AND  (@RoleId   IS NULL OR RoleId = @RoleId)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string GetOrgPermissions = @"
        SELECT Id, MenuId, PageId, PageModuleId, ActionId, RoleId, IsAllowed, OrgId, CreatedAt
        FROM   MenuAndPagePermissions
        WHERE  IsDeleted = 0
          AND  OrgId     = @OrgId
          AND  (@MenuId  IS NULL OR MenuId = @MenuId)
          AND  (@PageId  IS NULL OR PageId = @PageId)
          AND  (@RoleId  IS NULL OR RoleId = @RoleId)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountOrgPermissions = @"
        SELECT COUNT(*)
        FROM   MenuAndPagePermissions
        WHERE  IsDeleted = 0
          AND  OrgId     = @OrgId
          AND  (@MenuId  IS NULL OR MenuId = @MenuId)
          AND  (@PageId  IS NULL OR PageId = @PageId)
          AND  (@RoleId  IS NULL OR RoleId = @RoleId)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "MenuId", "PageId", "RoleId", "ActionId", "IsAllowed", "CreatedAt" };
    public const string DefaultSortColumn = "MenuId";
}
