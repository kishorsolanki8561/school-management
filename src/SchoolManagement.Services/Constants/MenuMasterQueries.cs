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
          AND  (@Search IS NULL OR Name LIKE '%' + @Search + '%')
        ORDER  BY Position, Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM   MenuMasters
        WHERE  IsDeleted = 0
          AND  (@Search IS NULL OR Name LIKE '%' + @Search + '%')";
}
