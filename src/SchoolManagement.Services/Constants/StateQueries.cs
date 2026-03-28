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
          AND (@Search   IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR s.IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR s.CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR s.CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM States s
        WHERE s.IsDeleted = 0
          AND (@Search   IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR s.IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR s.CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR s.CreatedAt <= @DateTo)";

    public const string GetByCountry = @"
        SELECT s.Id, s.Name, s.Code, s.CountryId, c.Name AS CountryName, s.IsActive, s.CreatedAt
        FROM States s
        INNER JOIN Countries c ON c.Id = s.CountryId
        WHERE s.CountryId = @CountryId AND s.IsDeleted = 0
          AND (@Search   IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR s.IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR s.CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR s.CreatedAt <= @DateTo)";

    public const string CountByCountry = @"
        SELECT COUNT(*)
        FROM States s
        WHERE s.CountryId = @CountryId AND s.IsDeleted = 0
          AND (@Search   IS NULL OR s.Name LIKE '%' + @Search + '%' OR s.Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR s.IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR s.CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR s.CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "s.Id", "s.Name", "s.Code", "s.IsActive", "s.CreatedAt" };
    public const string DefaultSortColumn = "s.Name";
}
