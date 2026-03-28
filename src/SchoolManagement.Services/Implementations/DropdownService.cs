using System.Text;
using System.Text.Json;
using Dapper;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;
using SchoolManagement.Common.Constants;

namespace SchoolManagement.Services.Implementations;

public sealed class DropdownService : IDropdownService
{
    private readonly IReadRepository _readRepository;

    public DropdownService(IReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<IEnumerable<Dictionary<string, object?>>> GetDropdownAsync(
        DropdownRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── 1. Resolve definition ────────────────────────────────────────────────
        if (!DropdownRegistry.Definitions.TryGetValue(request.Key, out var def))
            throw new ArgumentException(AppMessages.Dropdown.UnknownKey(request.Key.ToString()));

        var extraColumns  = request.ExtraColumns  ?? Array.Empty<string>();
        var filters       = request.Filters       ?? new Dictionary<string, JsonElement>();

        // ── 2. Whitelist validation ──────────────────────────────────────────────
        var invalidExtras = extraColumns
            .Where(c => !def.AllowedExtraColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (invalidExtras.Length > 0)
            throw new ArgumentException(AppMessages.Dropdown.InvalidExtraColumns(invalidExtras));

        var invalidFilters = filters.Keys
            .Where(k => !def.AllowedFilterColumns.Contains(k, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (invalidFilters.Length > 0)
            throw new ArgumentException(AppMessages.Dropdown.InvalidFilterColumns(invalidFilters));

        // ── 3. Build SQL + parameters ────────────────────────────────────────────
        var sql = BuildSql(def, extraColumns, filters);

        DynamicParameters? parameters = null;
        if (filters.Count > 0)
        {
            parameters = new DynamicParameters();
            foreach (var (key, element) in filters)
                parameters.Add(key, ExtractValue(element));
        }

        // ── 4. Execute ───────────────────────────────────────────────────────────
        var rows = await _readRepository.QueryDynamicAsync(sql, parameters);

        // ── 5. Project dynamic rows to typed dictionaries (camelCase keys) ───────
        return rows.Select(row =>
        {
            var dict = (IDictionary<string, object>)row;
            return dict.ToDictionary(
                kv => kv.Key,   // Dapper already uses the alias from SELECT AS [name]
                kv => (object?)kv.Value,
                StringComparer.OrdinalIgnoreCase);
        });
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string BuildSql(
        DropdownDefinition              def,
        string[]                        extraColumns,
        Dictionary<string, JsonElement> filters)
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT [{def.NameColumn}] AS [name], [{def.ValueColumn}] AS [value]");

        foreach (var col in extraColumns)
            sb.Append($", [{col}] AS [{ToCamelCase(col)}]");

        sb.Append($" FROM [{def.TableName}] WHERE {def.BaseCondition}");

        foreach (var key in filters.Keys)
            sb.Append($" AND [{key}] = @{key}");

        sb.Append(" ORDER BY [name]");
        return sb.ToString();
    }

    /// <summary>Converts "CountryId" → "countryId".</summary>
    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    /// <summary>Extracts a CLR value from a <see cref="JsonElement"/>.</summary>
    private static object? ExtractValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String  => element.GetString(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => null,
        JsonValueKind.Number  =>
            element.TryGetInt32(out var i)  ? (object)i  :
            element.TryGetInt64(out var l)  ? (object)l  :
            element.TryGetDecimal(out var d) ? (object)d :
            (object)element.GetDouble(),
        _ => element.ToString()
    };
}
