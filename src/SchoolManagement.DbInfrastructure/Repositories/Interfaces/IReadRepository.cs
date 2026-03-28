using SchoolManagement.Models.Common;

namespace SchoolManagement.DbInfrastructure.Repositories.Interfaces;

public interface IReadRepository
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
    Task<PagedResult<T>> QueryPagedAsync<T>(string sql, string countSql, object? param = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<dynamic>> QueryDynamicAsync(string sql, object? param = null);
}
