namespace SchoolManagement.Services.Constants;

internal static class CityQueries
{
    public const string GetById = @"
        SELECT ci.Id, ci.Name, ci.StateId, s.Name AS StateName, s.CountryId, c.Name AS CountryName, ci.IsActive, ci.CreatedAt
        FROM Cities ci
        INNER JOIN States s ON s.Id = ci.StateId
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE ci.Id = @Id AND ci.IsDeleted = 0";

    public const string GetAll = @"
        SELECT ci.Id, ci.Name, ci.StateId, s.Name AS StateName, s.CountryId, c.Name AS CountryName, ci.IsActive, ci.CreatedAt
        FROM Cities ci
        INNER JOIN States s ON s.Id = ci.StateId
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE ci.IsDeleted = 0
          AND (@Search IS NULL OR ci.Name LIKE '%' + @Search + '%')
        ORDER BY ci.Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM Cities ci
        WHERE ci.IsDeleted = 0
          AND (@Search IS NULL OR ci.Name LIKE '%' + @Search + '%')";

    public const string GetByState = @"
        SELECT ci.Id, ci.Name, ci.StateId, s.Name AS StateName, s.CountryId, c.Name AS CountryName, ci.IsActive, ci.CreatedAt
        FROM Cities ci
        INNER JOIN States s ON s.Id = ci.StateId
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE ci.StateId = @StateId AND ci.IsDeleted = 0
        ORDER BY ci.Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByState = @"
        SELECT COUNT(*)
        FROM Cities
        WHERE StateId = @StateId AND IsDeleted = 0";
}
