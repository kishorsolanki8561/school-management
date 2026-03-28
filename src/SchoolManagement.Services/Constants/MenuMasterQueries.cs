namespace SchoolManagement.Services.Constants;

internal static class MenuMasterQueries
{
    public const string GetById = @"
        SELECT m.Id, m.Name, m.HasChild, m.ParentMenuId, pm.Name AS ParentMenuName,
               m.Position, m.IconClass, m.IsActive, m.IsUseMenuForOwnerAdmin, m.CreatedAt
        FROM   MenuMasters m
        LEFT JOIN MenuMasters pm ON pm.Id = m.ParentMenuId AND pm.IsDeleted = 0
        WHERE  m.Id        = @Id
          AND  m.IsDeleted = 0";

    public const string GetAll = @"
        SELECT m.Id, m.Name, m.HasChild, m.ParentMenuId, pm.Name AS ParentMenuName,
               m.Position, m.IconClass, m.IsActive, m.IsUseMenuForOwnerAdmin, m.CreatedAt
        FROM   MenuMasters m
        LEFT JOIN MenuMasters pm ON pm.Id = m.ParentMenuId AND pm.IsDeleted = 0
        WHERE  m.IsDeleted = 0
          AND  (@Search   IS NULL OR m.Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR m.IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR m.CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR m.CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   MenuMasters m
        WHERE  m.IsDeleted = 0
          AND  (@Search   IS NULL OR m.Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR m.IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR m.CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR m.CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "HasChild", "ParentMenuName", "Position", "IsActive", "CreatedAt" };
    public const string DefaultSortColumn = "Position";
}
