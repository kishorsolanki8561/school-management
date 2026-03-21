namespace SchoolManagement.Models.Common;

public sealed class PaginationRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }

    // Date range filter
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }

    // Status filter (maps to enum int values)
    public int? Status { get; init; }

    // Additional dynamic key-value filters
    public Dictionary<string, string>? Filters { get; init; }

    public int Offset => (Page - 1) * PageSize;
}
