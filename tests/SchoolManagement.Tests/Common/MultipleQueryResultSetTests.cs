using FluentAssertions;
using SchoolManagement.Common.Generic;
using Xunit;

namespace SchoolManagement.Tests.Common;

public sealed class MultipleQueryResultSetTests
{
    [Fact]
    public void TwoGeneric_ShouldHoldBothResultSets()
    {
        var set1 = new List<string> { "a", "b" };
        var set2 = new List<int> { 1, 2, 3 };

        var result = new MultipleQueryResultSet<string, int>(set1, set2);

        result.Set1.Should().HaveCount(2);
        result.Set2.Should().HaveCount(3);
    }

    [Fact]
    public void ThreeGeneric_ShouldHoldAllThreeSets()
    {
        var set1 = new List<string> { "x" };
        var set2 = new List<int> { 42 };
        var set3 = new List<bool> { true, false };

        var result = new MultipleQueryResultSet<string, int, bool>(set1, set2, set3);

        result.Set3.Should().HaveCount(2);
    }

    [Fact]
    public void EmptySets_ShouldBeAllowed()
    {
        var result = new MultipleQueryResultSet<string, int>(new List<string>(), new List<int>());

        result.Set1.Should().BeEmpty();
        result.Set2.Should().BeEmpty();
    }
}
