namespace SchoolManagement.Services.Constants;

internal static class CountryQueries
{
    public const string GetById = @"
        SELECT Id, Name, Code, IsActive, CreatedAt
        FROM Countries
        WHERE Id = @Id AND IsDeleted = 0";

    // Note: ORDER BY + OFFSET/FETCH appended dynamically by QueryBuilder in the service
    public const string GetAll = @"
        SELECT Id, Name, Code, IsActive, CreatedAt
        FROM Countries
        WHERE IsDeleted = 0
          AND (@Search   IS NULL OR Name LIKE '%' + @Search + '%' OR Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM Countries
        WHERE IsDeleted = 0
          AND (@Search   IS NULL OR Name LIKE '%' + @Search + '%' OR Code LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "Code", "IsActive", "CreatedAt" };
    public const string DefaultSortColumn = "Name";
}
