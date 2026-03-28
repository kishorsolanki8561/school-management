using FluentAssertions;
using SchoolManagement.Services.Helpers;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class QueryBuilderTests
{
    private const string BaseSql = "SELECT Id, Name FROM Countries WHERE IsDeleted = 0";
    private static readonly string[] Allowed = { "Id", "Name", "CreatedAt" };

    // ── Default sort ──────────────────────────────────────────────────────────

    [Fact]
    public void AppendPaging_NullSortBy_UsesDefaultColumnAsc()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, null, false, Allowed, "Name");

        result.Should().Contain("ORDER BY Name ASC");
        result.Should().Contain("OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
    }

    [Fact]
    public void AppendPaging_NullSortBy_DefaultSortDescending_UsesDefaultColumnDesc()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, null, false, Allowed, "Timestamp", defaultSortDescending: true);

        result.Should().Contain("ORDER BY Timestamp DESC");
    }

    // ── Valid sort column ─────────────────────────────────────────────────────

    [Fact]
    public void AppendPaging_ValidSortBy_UsesSpecifiedColumn()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "CreatedAt", false, Allowed, "Name");

        result.Should().Contain("ORDER BY CreatedAt ASC");
    }

    [Fact]
    public void AppendPaging_ValidSortBy_SortDescendingTrue_UsesDESC()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "Name", true, Allowed, "Id");

        result.Should().Contain("ORDER BY Name DESC");
    }

    // ── Invalid / SQL-injection attempt ──────────────────────────────────────

    [Fact]
    public void AppendPaging_InvalidSortBy_FallsBackToDefault()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "'; DROP TABLE Countries;--", false, Allowed, "Name");

        result.Should().Contain("ORDER BY Name ASC");
        result.Should().NotContain("DROP TABLE");
    }

    [Fact]
    public void AppendPaging_UnknownColumn_FallsBackToDefault()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "HackerCol", false, Allowed, "Name");

        result.Should().Contain("ORDER BY Name ASC");
        result.Should().NotContain("HackerCol");
    }

    // ── Case-insensitive column matching ─────────────────────────────────────

    [Fact]
    public void AppendPaging_SortByCaseInsensitive_Matches()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "createdat", false, Allowed, "Name");

        result.Should().Contain("ORDER BY CreatedAt ASC");
    }

    // ── Base SQL is preserved ─────────────────────────────────────────────────

    [Fact]
    public void AppendPaging_BaseSqlIsPreservedUnchanged()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, "Name", false, Allowed, "Name");

        result.Should().StartWith(BaseSql);
    }

    // ── Pagination clause is always appended ─────────────────────────────────

    [Fact]
    public void AppendPaging_AlwaysAppendsPaginationClause()
    {
        var result = QueryBuilder.AppendPaging(BaseSql, null, false, Allowed, "Name");

        result.Should().Contain("OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
    }
}
