namespace SchoolManagement.Services.Constants;

internal static class StateQueries
{
    public const string GetById = @"
        SELECT s.Id, s.Name, s.Code, s.CountryId, c.Name AS CountryName, s.IsActive, s.CreatedAt
        FROM States s
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE s.Id = @Id AND s.IsDeleted = 0";

    public const string GetAll = @"
        SELECT s.Id, s.Name, s.Code, s.CountryId, c.Name AS CountryName, s.IsActive, s.CreatedAt
        FROM States s
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE s.IsDeleted = 0
          AND (@Search IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')
        ORDER BY s.Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM States s
        WHERE s.IsDeleted = 0
          AND (@Search IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')";

    public const string GetByCountry = @"
        SELECT s.Id, s.Name, s.Code, s.CountryId, c.Name AS CountryName, s.IsActive, s.CreatedAt
        FROM States s
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE s.CountryId = @CountryId AND s.IsDeleted = 0
        ORDER BY s.Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountByCountry = @"
        SELECT COUNT(*)
        FROM States
        WHERE CountryId = @CountryId AND IsDeleted = 0";
}
