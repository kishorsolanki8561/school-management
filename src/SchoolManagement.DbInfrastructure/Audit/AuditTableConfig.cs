namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Audit configuration for one database table.
/// Only the columns listed here will appear in audit logs for this table.
/// </summary>
public sealed class AuditTableConfig
{
    public IReadOnlyList<AuditColumnConfig> Columns { get; }

    public AuditTableConfig(params AuditColumnConfig[] columns)
    {
        Columns = columns;
    }
}
