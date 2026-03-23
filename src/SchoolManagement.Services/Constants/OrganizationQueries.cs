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
          AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')
        ORDER BY Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    public const string CountAll = @"
        SELECT COUNT(*)
        FROM Organizations
        WHERE IsDeleted = 0
          AND (@Search IS NULL OR Name LIKE '%' + @Search + '%' OR Address LIKE '%' + @Search + '%')";
}
