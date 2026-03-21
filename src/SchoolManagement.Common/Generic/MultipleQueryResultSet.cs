namespace SchoolManagement.Common.Generic;

/// <summary>Holds two typed result sets from a Dapper QueryMultiple call.</summary>
public sealed class MultipleQueryResultSet<T1, T2>
{
    public IReadOnlyList<T1> Set1 { get; init; }
    public IReadOnlyList<T2> Set2 { get; init; }

    public MultipleQueryResultSet(IReadOnlyList<T1> set1, IReadOnlyList<T2> set2)
    {
        Set1 = set1;
        Set2 = set2;
    }
}

/// <summary>Holds three typed result sets from a Dapper QueryMultiple call.</summary>
public sealed class MultipleQueryResultSet<T1, T2, T3>
{
    public IReadOnlyList<T1> Set1 { get; init; }
    public IReadOnlyList<T2> Set2 { get; init; }
    public IReadOnlyList<T3> Set3 { get; init; }

    public MultipleQueryResultSet(IReadOnlyList<T1> set1, IReadOnlyList<T2> set2, IReadOnlyList<T3> set3)
    {
        Set1 = set1;
        Set2 = set2;
        Set3 = set3;
    }
}

/// <summary>Holds four typed result sets from a Dapper QueryMultiple call.</summary>
public sealed class MultipleQueryResultSet<T1, T2, T3, T4>
{
    public IReadOnlyList<T1> Set1 { get; init; }
    public IReadOnlyList<T2> Set2 { get; init; }
    public IReadOnlyList<T3> Set3 { get; init; }
    public IReadOnlyList<T4> Set4 { get; init; }

    public MultipleQueryResultSet(IReadOnlyList<T1> set1, IReadOnlyList<T2> set2,
        IReadOnlyList<T3> set3, IReadOnlyList<T4> set4)
    {
        Set1 = set1;
        Set2 = set2;
        Set3 = set3;
        Set4 = set4;
    }
}

/// <summary>Holds five typed result sets from a Dapper QueryMultiple call.</summary>
public sealed class MultipleQueryResultSet<T1, T2, T3, T4, T5>
{
    public IReadOnlyList<T1> Set1 { get; init; }
    public IReadOnlyList<T2> Set2 { get; init; }
    public IReadOnlyList<T3> Set3 { get; init; }
    public IReadOnlyList<T4> Set4 { get; init; }
    public IReadOnlyList<T5> Set5 { get; init; }

    public MultipleQueryResultSet(IReadOnlyList<T1> set1, IReadOnlyList<T2> set2,
        IReadOnlyList<T3> set3, IReadOnlyList<T4> set4, IReadOnlyList<T5> set5)
    {
        Set1 = set1;
        Set2 = set2;
        Set3 = set3;
        Set4 = set4;
        Set5 = set5;
    }
}
