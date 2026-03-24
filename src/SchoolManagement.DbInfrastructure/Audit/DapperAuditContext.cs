namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Carries the audit metadata required when a Dapper write (INSERT / UPDATE / DELETE)
/// should produce an <c>AuditLog</c> row.
/// </summary>
public sealed class DapperAuditContext
{
    /// <summary>EF Core / DB table name (e.g. "Organizations").</summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>Primary key of the affected row as a string.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>"Created", "Updated", or "Deleted".</summary>
    public string Action { get; init; } = "Updated";

    /// <summary>
    /// Entity snapshot <b>before</b> the change.
    /// Required for "Updated" (diff) and "Deleted" (full snapshot).
    /// </summary>
    public object? OldEntity { get; init; }

    /// <summary>
    /// Entity snapshot <b>after</b> the change.
    /// Required for "Created" (full snapshot) and "Updated" (diff).
    /// </summary>
    public object? NewEntity { get; init; }
}
