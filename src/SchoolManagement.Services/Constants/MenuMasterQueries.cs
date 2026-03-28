namespace SchoolManagement.Services.Constants;

internal static class MenuMasterQueries
{
    public const string GetById = @"
        SELECT Id, Name, HasChild, ParentMenuId, Position, IconClass, IsActive, IsUseMenuForOwnerAdmin, CreatedAt
        FROM   MenuMasters
        WHERE  Id        = @Id
          AND  IsDeleted = 0";

    public const string GetAll = @"
        SELECT Id, Name, HasChild, ParentMenuId, Position, IconClass, IsActive, IsUseMenuForOwnerAdmin, CreatedAt
        FROM   MenuMasters
        WHERE  IsDeleted = 0
          AND  (@Search   IS NULL OR Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   MenuMasters
        WHERE  IsDeleted = 0
          AND  (@Search   IS NULL OR Name LIKE '%' + @Search + '%')
          AND  (@IsActive IS NULL OR IsActive  = @IsActive)
          AND  (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND  (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "Position", "IsActive", "CreatedAt" };
    public const string DefaultSortColumn = "Position";
}
