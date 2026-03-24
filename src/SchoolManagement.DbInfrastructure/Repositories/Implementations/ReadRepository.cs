using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;

namespace SchoolManagement.DbInfrastructure.Repositories.Implementations;

public sealed class ReadRepository : IReadRepository
{
    private readonly string _connectionString;

    public ReadRepository()
    {
        _connectionString = AppConfigFactory.Configuration?
            .GetSection("ConnectionStrings:DefaultConnection")?.Value
            ?? throw new InvalidOperationException(AppMessages.General.ConnectionStringMissing);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        var query = BuildExecutedQuery(sql, param);
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<T>(query);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        var query = BuildExecutedQuery(sql, param);
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<T>(query);
    }

    public async Task<PagedResult<T>> QueryPagedAsync<T>(string sql, string countSql, object? param = null, int page = 1, int pageSize = 20)
    {
        var query      = BuildExecutedQuery(sql, param);
        var countQuery = BuildExecutedQuery(countSql, param);
        await using var connection = new SqlConnection(_connectionString);
        var items = await connection.QueryAsync<T>(query);
        var total = await connection.ExecuteScalarAsync<int>(countQuery);
        return PagedResult<T>.Create(items, total, page, pageSize);
    }

    /// <summary>
    /// Converts a Dapper parameter object (anonymous type, DynamicParameters, or POCO)
    /// into a human-readable key=value list — useful for logging and debugging.
    /// Example output: @RoleId = 2, @PageId = 5, @Ids = [1, 2, 3]
    /// </summary>
    public static string ConvertToParameterString(object? param)
    {
        if (param is null) return "(no parameters)";

        var dict = ExtractParameters(param);
        if (dict.Count == 0) return "(no parameters)";

        var sb = new StringBuilder();
        foreach (var (name, value) in dict)
        {
            if (sb.Length > 0) sb.Append(", ");
            var formatted = value is IEnumerable enumerable && value is not string
                ? $"[{string.Join(", ", enumerable.Cast<object?>().Select(FormatSqlValue))}]"
                : FormatSqlValue(value);
            sb.Append('@').Append(name).Append(" = ").Append(formatted);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Replaces every @Placeholder in <paramref name="sql"/> with its formatted value
    /// from <paramref name="param"/>, returning a ready-to-copy-paste executable query.
    /// Collections are expanded inline: WHERE Id IN (1, 2, 3).
    /// </summary>
    /// <example>
    /// BuildExecutedQuery("SELECT * FROM Roles WHERE Id = @RoleId AND IsDeleted = @IsDeleted",
    ///                    new { RoleId = 3, IsDeleted = false })
    /// → "SELECT * FROM Roles WHERE Id = 3 AND IsDeleted = 0"
    /// </example>
    public static string BuildExecutedQuery(string sql, object? param = null)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        if (param is null) return sql;

        // Build a name→value dictionary (longest names first to avoid partial replacements,
        // e.g. @RoleIdList before @RoleId)
        var dict = ExtractParameters(param);
        if (dict.Count == 0) return sql;

        // Sort descending by key length so @RoleIdList is replaced before @RoleId
        foreach (var key in dict.Keys.OrderByDescending(k => k.Length))
        {
            var pattern = $@"@{Regex.Escape(key)}\b";
            var replacement = dict[key] is IEnumerable enumerable && dict[key] is not string
                ? $"({string.Join(", ", enumerable.Cast<object?>().Select(FormatSqlValue))})"
                : FormatSqlValue(dict[key]);

            sql = Regex.Replace(sql, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return sql;
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>Extracts name→raw-value pairs from anonymous types, POCOs, or DynamicParameters.</summary>
    private static Dictionary<string, object?> ExtractParameters(object param)
    {
        if (param is DynamicParameters dp)
            return dp.ParameterNames.ToDictionary(
                name => name,
                name => (object?)dp.Get<object?>(name),
                StringComparer.OrdinalIgnoreCase);

        return param.GetType()
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .ToDictionary(
                        p => p.Name,
                        p => (object?)p.GetValue(param),
                        StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Formats a single scalar value as a SQL literal.</summary>
    private static string FormatSqlValue(object? value) => value switch
    {
        null                   => "NULL",
        string s               => $"'{s.Replace("'", "''")}'",   // escape embedded quotes
        bool b                 => b ? "1" : "0",
        DateTime dt            => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
        DateTimeOffset dto     => $"'{dto:yyyy-MM-dd HH:mm:ss zzz}'",
        Enum e                 => Convert.ToInt32(e).ToString(),
        _                      => value.ToString() ?? "NULL"
    };

}
