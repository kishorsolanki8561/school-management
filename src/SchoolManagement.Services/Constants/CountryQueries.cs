namespace SchoolManagement.Services.Constants;

internal static class CountryQueries
{
    public const string GetById = @"
        SELECT Id, Name, Code, IsActive, CreatedAt
        FROM Countries
        WHERE Id = @Id AND IsDeleted = 0";

    public const string GetAll = @"
        SELECT Id, Name, Code, IsActive, CreatedAt
        FROM Countries
        WHERE IsDeleted = 0
          AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Code LIKE '%' + @Search + '%')
        ORDER BY Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM Countries
        WHERE IsDeleted = 0
          AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Code LIKE '%' + @Search + '%')";
}
