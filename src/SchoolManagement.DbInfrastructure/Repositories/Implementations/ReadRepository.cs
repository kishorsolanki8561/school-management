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
}
