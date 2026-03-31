namespace SchoolManagement.Services.Constants;

internal static class UserManagementQueries
{
    /// <summary>
    /// Returns a single user with their org-scoped roles.
    /// When @OrgId IS NULL (OwnerAdmin), roles with any OrgId are returned.
    /// When @OrgId is set, only roles scoped to that org (or system-level null) are returned.
    /// </summary>
    public const string GetById = @"
        SELECT u.Id, u.Username, u.Email, u.IsActive, u.CreatedAt
        FROM   Users u
        WHERE  u.Id = @Id AND u.IsDeleted = 0";

    public const string GetRolesByUserId = @"
        SELECT urm.RoleId, r.Name AS RoleName, urm.OrgId
        FROM   UserRoleMappings urm
        INNER JOIN Roles r ON r.Id = urm.RoleId AND r.IsDeleted = 0
        WHERE  urm.UserId    = @UserId
          AND  urm.IsDeleted = 0
          AND  (@OrgId IS NULL OR urm.OrgId = @OrgId OR urm.OrgId IS NULL)";

    /// <summary>
    /// Lists users in a specific org (via UserOrganizationMappings).
    /// When @OrgId IS NULL (OwnerAdmin), all users are returned.
    /// </summary>
    public const string GetAll = @"
        SELECT DISTINCT u.Id, u.Username, u.Email, u.IsActive, u.CreatedAt
        FROM   Users u
        LEFT JOIN UserOrganizationMappings uom
               ON uom.UserId = u.Id AND uom.IsDeleted = 0
        WHERE  u.IsDeleted = 0
          AND  (@OrgId     IS NULL OR uom.OrgId = @OrgId)
          AND  (@Search    IS NULL OR u.Username LIKE '%' + @Search + '%'
                                   OR u.Email    LIKE '%' + @Search + '%')
          AND  (@IsActive  IS NULL OR u.IsActive = @IsActive)
          AND  (@DateFrom  IS NULL OR u.CreatedAt >= @DateFrom)
          AND  (@DateTo    IS NULL OR u.CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(DISTINCT u.Id)
        FROM   Users u
        LEFT JOIN UserOrganizationMappings uom
               ON uom.UserId = u.Id AND uom.IsDeleted = 0
        WHERE  u.IsDeleted = 0
          AND  (@OrgId     IS NULL OR uom.OrgId = @OrgId)
          AND  (@Search    IS NULL OR u.Username LIKE '%' + @Search + '%'
                                   OR u.Email    LIKE '%' + @Search + '%')
          AND  (@IsActive  IS NULL OR u.IsActive = @IsActive)
          AND  (@DateFrom  IS NULL OR u.CreatedAt >= @DateFrom)
          AND  (@DateTo    IS NULL OR u.CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Username", "Email", "IsActive", "CreatedAt" };
    public const string DefaultSortColumn = "Username";
}
