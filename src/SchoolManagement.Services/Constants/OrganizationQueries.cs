namespace SchoolManagement.Services.Constants;

internal static class OrganizationQueries
{
    public const string GetById = @"
        SELECT Id, Name, Address, IsActive, CreatedAt
        FROM Organizations
        WHERE Id = @Id AND IsDeleted = 0";

    public const string GetAll = @"
        SELECT Id, Name, Address, IsActive, CreatedAt
        FROM Organizations
        WHERE IsDeleted = 0
          AND (@Search   IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM Organizations
        WHERE IsDeleted = 0
          AND (@Search   IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')
          AND (@IsActive IS NULL OR IsActive  = @IsActive)
          AND (@DateFrom IS NULL OR CreatedAt >= @DateFrom)
          AND (@DateTo   IS NULL OR CreatedAt <= @DateTo)";

    public static readonly string[] AllowedSortColumns = { "Id", "Name", "Address", "IsActive", "CreatedAt" };
    public const string DefaultSortColumn = "Name";
}
