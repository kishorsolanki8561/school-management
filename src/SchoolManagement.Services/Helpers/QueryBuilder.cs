namespace SchoolManagement.Services.Helpers;

/// <summary>
/// Safely appends ORDER BY + OFFSET/FETCH pagination to a base SQL string.
/// The <paramref name="sortBy"/> value is validated against <paramref name="allowedColumns"/>
/// (case-insensitive). Falls back to <paramref name="defaultColumn"/> if invalid or null.
/// Column names are never taken from raw user input — only whitelisted values are injected.
/// </summary>
internal static class QueryBuilder
{
    public static string AppendPaging(
        string   sql,
        string?  sortBy,
        bool     sortDescending,
        string[] allowedColumns,
        string   defaultColumn,
        bool     defaultSortDescending = false)
    {
        var matched = allowedColumns.FirstOrDefault(c => string.Equals(c, sortBy, StringComparison.OrdinalIgnoreCase));
        var isDefault = matched is null;
        var col = isDefault ? defaultColumn : matched!;
        var dir = (isDefault ? defaultSortDescending : sortDescending) ? "DESC" : "ASC";

        return $"{sql}\n        ORDER BY {col} {dir}\n        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
    }
}
