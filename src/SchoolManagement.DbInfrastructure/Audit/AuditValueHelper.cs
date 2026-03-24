using Microsoft.EntityFrameworkCore;
using SchoolManagement.Models.Entities;
using System.Text.Json;

namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Shared static helpers for building audit log data.
/// Used by both <c>AuditInterceptor</c> (EF Core path) and
/// <c>DapperAuditExecutor</c> (Dapper write path).
/// </summary>
internal static class AuditValueHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    // ── Column list ───────────────────────────────────────────────────────────

    /// <summary>
    /// Merges the table's explicit columns with <see cref="AuditConfiguration.DefaultColumns"/>.
    /// Explicit entries win on duplicate PropertyName (case-insensitive).
    /// </summary>
    public static IReadOnlyList<AuditColumnConfig> GetEffectiveColumns(AuditTableConfig config)
    {
        var explicitNames = config.Columns
            .Select(c => c.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return config.Columns
            .Concat(AuditConfiguration.DefaultColumns.Where(d => !explicitNames.Contains(d.PropertyName)))
            .ToList();
    }

    // ── Value formatting ──────────────────────────────────────────────────────

    /// <summary>
    /// Converts a raw CLR string value to its display form.
    /// Booleans become the column's <c>BoolTrueDisplay</c> / <c>BoolFalseDisplay</c>
    /// (default "Yes" / "No"). Non-boolean values pass through unchanged.
    /// </summary>
    public static string? FormatValue(AuditColumnConfig col, string? rawValue)
    {
        if (rawValue is null) return null;
        if (rawValue.Equals("True",  StringComparison.OrdinalIgnoreCase)) return col.BoolTrueDisplay;
        if (rawValue.Equals("False", StringComparison.OrdinalIgnoreCase)) return col.BoolFalseDisplay;
        return rawValue;
    }

    /// <summary>
    /// Returns <c>true</c> when a column value should be excluded from the audit JSON:
    /// <list type="bullet">
    ///   <item>The display value is null.</item>
    ///   <item><c>IsDeleted</c> is <c>false</c> — the default/normal state, not worth recording.</item>
    /// </list>
    /// </summary>
    public static bool ShouldSkip(AuditColumnConfig col, string? rawValue, string? display)
    {
        if (display is null) return true;

        if (col.PropertyName.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase)
            && rawValue?.Equals("False", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }

    // ── FK resolution ─────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a FK integer ID to the referenced entity's display value
    /// (e.g. RoleId=3 → "Admin"). Checks EF identity map first; falls back to a DB query.
    /// Returns the raw ID string when the entity or property cannot be found.
    /// </summary>
    public static async Task<string?> ResolveLookupAsync(
        DbContext     context,
        AuditLookup   lookup,
        int           id,
        CancellationToken ct)
    {
        var entityType = context.Model
            .GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == lookup.EntityTypeName);

        if (entityType is null) return id.ToString();

        var entity = await context.FindAsync(entityType.ClrType, new object?[] { id }, ct);
        if (entity is null) return id.ToString();

        var prop = entityType.ClrType.GetProperty(lookup.ValueProperty);
        return prop?.GetValue(entity)?.ToString() ?? id.ToString();
    }

    // ── Entity snapshot builder ───────────────────────────────────────────────

    /// <summary>
    /// Reads property values from <paramref name="entity"/> via reflection,
    /// resolves FK lookups, applies formatting and skip rules, then returns a
    /// display-name keyed dictionary ready for JSON serialisation.
    /// Returns <c>null</c> when the entity is <c>null</c> or no columns survive filtering.
    /// </summary>
    public static async Task<Dictionary<string, string?>?> BuildFromEntityAsync(
        object?           entity,
        AuditTableConfig  tableConfig,
        DbContext         context,
        CancellationToken ct)
    {
        if (entity is null) return null;

        var effectiveCols = GetEffectiveColumns(tableConfig);
        var entityType    = entity.GetType();
        var result        = new Dictionary<string, string?>(effectiveCols.Count);

        foreach (var col in effectiveCols)
        {
            var clrProp = entityType.GetProperty(col.PropertyName);
            if (clrProp is null) continue;

            var rawValue = clrProp.GetValue(entity)?.ToString();
            string? display;

            if (col.Lookup is not null && int.TryParse(rawValue, out var id) && id != 0)
                display = await ResolveLookupAsync(context, col.Lookup, id, ct);
            else
                display = FormatValue(col, rawValue);

            if (ShouldSkip(col, rawValue, display)) continue;

            result[col.DisplayName] = display;
        }

        return result.Count > 0 ? result : null;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    public static string? Serialize(Dictionary<string, string?>? dict)
        => dict is { Count: > 0 } ? JsonSerializer.Serialize(dict, _jsonOptions) : null;
}
