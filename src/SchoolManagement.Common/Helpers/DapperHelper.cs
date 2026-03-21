using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.Models.Common;

namespace SchoolManagement.Common.Helpers;

public interface IDapperHelper
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
    Task<PagedResult<T>> QueryPagedAsync<T>(string sql, string countSql, object? param = null, int page = 1, int pageSize = 20);
    Task<int> ExecuteAsync(string sql, object? param = null);
    Task<(IEnumerable<T1> first, IEnumerable<T2> second)> QueryMultipleAsync<T1, T2>(string sql, object? param = null);
    Task<(IEnumerable<T1> first, IEnumerable<T2> second, IEnumerable<T3> third)> QueryMultipleAsync<T1, T2, T3>(string sql, object? param = null);

    Task<Dictionary<(Type Type, string Alias), IEnumerable<object>>> GetMultipleDatasetAsync(
        string sql,
        DynamicParameters parameters,
        params (Type Type, string Alias)[] datasets);

    Task<Dictionary<Type, IEnumerable<object>>> GetMultipleDatasetAsync(
        string sql,
        DynamicParameters parameters,
        params Type[] resultTypes);

    List<T> GetListingType<T>(Dictionary<Type, IEnumerable<object>> dictionary);
    List<T> GetListingType<T>(Dictionary<(Type Type, string Alias), IEnumerable<object>> result, string alias);
}

public sealed class DapperHelper : IDapperHelper
{
    private readonly string _connectionString;

    public DapperHelper()
    {
        _connectionString = AppConfigFactory.Configuration?
            .GetSection("ConnectionStrings:DefaultConnection")?.Value
            ?? throw new InvalidOperationException(AppMessages.General.ConnectionStringMissing);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Core query API
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<T>(sql, param);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<PagedResult<T>> QueryPagedAsync<T>(string sql, string countSql, object? param = null, int page = 1, int pageSize = 20)
    {
        await using var connection = new SqlConnection(_connectionString);
        var items = await connection.QueryAsync<T>(sql, param);
        var total = await connection.ExecuteScalarAsync<int>(countSql, param);
        return PagedResult<T>.Create(items, total, page, pageSize);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, param);
    }

    public async Task<(IEnumerable<T1> first, IEnumerable<T2> second)> QueryMultipleAsync<T1, T2>(
        string sql, object? param = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(sql, param);
        var first = await multi.ReadAsync<T1>();
        var second = await multi.ReadAsync<T2>();
        return (first, second);
    }

    public async Task<(IEnumerable<T1> first, IEnumerable<T2> second, IEnumerable<T3> third)> QueryMultipleAsync<T1, T2, T3>(
        string sql, object? param = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(sql, param);
        var first = await multi.ReadAsync<T1>();
        var second = await multi.ReadAsync<T2>();
        var third = await multi.ReadAsync<T3>();
        return (first, second, third);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Dynamic multi-dataset API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes a multi-result-set query and returns each dataset keyed by
    /// (CLR Type, caller-supplied alias). Useful when the same type appears
    /// in more than one result set.
    /// </summary>
    public async Task<Dictionary<(Type Type, string Alias), IEnumerable<object>>> GetMultipleDatasetAsync(
        string sql,
        DynamicParameters parameters,
        params (Type Type, string Alias)[] datasets)
    {
        var result = new Dictionary<(Type, string), IEnumerable<object>>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var query = ConvertToParameterString(sql, parameters);
        using var multi = await connection.QueryMultipleAsync(query);
        foreach (var item in datasets)
        {
            var data = multi.Read(item.Type).Cast<object>().ToList();
            result[(item.Type, item.Alias)] = data;
        }
        return result;
    }

    /// <summary>
    /// Executes a multi-result-set query and returns each dataset keyed by CLR Type.
    /// Use when every result set maps to a distinct type.
    /// </summary>
    public async Task<Dictionary<Type, IEnumerable<object>>> GetMultipleDatasetAsync(
        string sql,
        DynamicParameters parameters,
        params Type[] resultTypes)
    {
        var result = new Dictionary<Type, IEnumerable<object>>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var query = ConvertToParameterString(sql, parameters);
        using var multi = await connection.QueryMultipleAsync(query);
        foreach (var type in resultTypes)
        {
            var data = multi.Read(type).Cast<object>().ToList();
            result[type] = data;
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Convenience extractors
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Extracts a strongly-typed list from a Type → IEnumerable&lt;object&gt; dictionary
    /// returned by the type-keyed overload of GetMultipleDatasetAsync.
    /// </summary>
    public List<T> GetListingType<T>(Dictionary<Type, IEnumerable<object>> dictionary)
    {
        if (!dictionary.TryGetValue(typeof(T), out var data))
            return new List<T>();
        return data.Cast<T>().ToList();
    }

    /// <summary>
    /// Extracts a strongly-typed list from a (Type, Alias) → IEnumerable&lt;object&gt; dictionary
    /// returned by the alias-keyed overload of GetMultipleDatasetAsync.
    /// </summary>
    public List<T> GetListingType<T>(
        Dictionary<(Type Type, string Alias), IEnumerable<object>> result,
        string alias)
    {
        return result
            .Where(x => x.Key.Type == typeof(T) && x.Key.Alias == alias)
            .SelectMany(x => x.Value)
            .Cast<T>()
            .ToList();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inlines DynamicParameters into the SQL string, replacing each @Param token
    /// with its formatted SQL literal. Required before passing to QueryMultipleAsync
    /// without a separate parameter bag.
    /// </summary>
    private static string ConvertToParameterString(string sql, DynamicParameters parameters)
    {
        if (parameters == null)
            return sql;

        var result = sql;
        foreach (var paramName in parameters.ParameterNames)
        {
            var value = parameters.Get<object>(paramName);
            var formattedValue = FormatSqlValue(value);
            result = Regex.Replace(
                result,
                $@"@{paramName}\b",
                formattedValue,
                RegexOptions.IgnoreCase);
        }
        return result;
    }

    /// <summary>
    /// Converts a CLR value to its SQL literal representation.
    /// </summary>
    private static string FormatSqlValue(object? value)
    {
        if (value == null)
            return "NULL";
        if (value is string || value is char)
            return $"'{value.ToString()!.Replace("'", "''")}'";
        if (value is DateTime dt)
            return $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'";
        if (value is bool b)
            return b ? "1" : "0";
        if (value is Enum)
            return Convert.ToInt32(value).ToString();
        if (value is Guid g)
            return $"'{g}'";
        return value.ToString()!;
    }
}
