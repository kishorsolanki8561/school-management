namespace SchoolManagement.Services.Constants;

internal static class AuthQueries
{
    /// <summary>
    /// Returns all active, non-deleted menus ordered by position.
    /// When @IsOwnerAdmin = 0, menus flagged IsUseMenuForOwnerAdmin are excluded.
    /// </summary>
    /// <summary>
    /// When @OrgId IS NULL (OwnerAdmin) → uses system-level permissions (mp.OrgId IS NULL).
    /// When @OrgId IS SET → uses org-specific permissions for this org's role copies.
    /// </summary>
    public const string GetDynamicMenus = @"
        SELECT DISTINCT MM.Id, Name, HasChild, ParentMenuId, Position, IconClass
        FROM   MenuMasters MM
        INNER  JOIN MenuAndPagePermissions mp
               ON  mp.MenuId    = MM.Id
               AND mp.IsDeleted = 0
               AND mp.IsAllowed = 1
               AND mp.RoleId    IN @RoleIds
               AND ((@OrgId IS NULL AND mp.OrgId IS NULL) OR mp.OrgId = @OrgId)
        WHERE  mm.IsDeleted = 0
          AND  mm.IsActive  = 1
        ORDER  BY Position, Name";

    /// <summary>
    /// Returns all active, non-deleted pages for which the role(s) have at least one
    /// IsAllowed = 1 permission. DISTINCT ensures a page is returned only once even
    /// when it has multiple allowed action rows.
    /// When @IsOwnerAdmin = 0, pages flagged IsUsePageForOwnerAdmin are excluded.
    /// </summary>
    public const string GetDynamicPages = @"
        SELECT DISTINCT
               p.Id,
               p.MenuId,
               p.Name,
               p.IconClass AS Icon,
               p.PageUrl
        FROM   PageMasters p
        INNER  JOIN MenuAndPagePermissions mp
               ON  mp.PageId    = p.Id
               AND mp.IsDeleted = 0
               AND mp.IsAllowed = 1
               AND mp.RoleId    IN @RoleIds
               AND ((@OrgId IS NULL AND mp.OrgId IS NULL) OR mp.OrgId = @OrgId)
        WHERE  p.IsDeleted = 0
          AND  p.IsActive  = 1
        ORDER  BY p.MenuId, p.Name";

    /// <summary>
    /// Returns one row per (module, action) pair that the role is allowed to perform.
    /// Group by (ModuleId, ModuleName, PageId) in C# to build DynamicModuleResponse.
    /// </summary>
    public const string GetDynamicModules = @"
        SELECT DISTINCT
               pm.Id         AS ModuleId,
               pm.Name       AS ModuleName,
               pm.PageId,
               mp.ActionId
        FROM   PageMasterModules pm
        INNER  JOIN MenuAndPagePermissions mp
               ON  mp.PageModuleId = pm.Id
               AND mp.PageId       = pm.PageId
               AND mp.IsDeleted    = 0
               AND mp.IsAllowed    = 1
               AND mp.RoleId       IN @RoleIds
               AND ((@OrgId IS NULL AND mp.OrgId IS NULL) OR mp.OrgId = @OrgId)
        WHERE  pm.IsDeleted = 0
          AND  pm.IsActive  = 1
        ORDER  BY pm.PageId, pm.Id, mp.ActionId";
}
