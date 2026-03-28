using System.Text.Json;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

public sealed class DropdownRequest
{
    /// <summary>Identifies which dropdown table/query to use.</summary>
    public DropdownKey Key { get; init; }

    /// <summary>
    /// Optional additional column names to include in the response.
    /// Only columns whitelisted in <see cref="DropdownRegistry"/> are accepted.
    /// </summary>
    public string[]? ExtraColumns { get; init; }

    /// <summary>
    /// Optional WHERE-clause filters. Keys must be whitelisted in <see cref="DropdownRegistry"/>.
    /// Values are typed JSON elements (string, number, bool, null).
    /// </summary>
    public Dictionary<string, JsonElement>? Filters { get; init; }
}
