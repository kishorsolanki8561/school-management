namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Audit configuration for a single entity property.
/// </summary>
public sealed class AuditColumnConfig
{
    /// <summary>Actual C# property name on the entity (e.g. "RoleId", "Username").</summary>
    public string PropertyName { get; }

    /// <summary>Human-readable label stored as the key in the audit JSON (e.g. "Role", "Username").</summary>
    public string DisplayName { get; }

    /// <summary>
    /// Optional FK resolution.  When set, the raw integer ID is replaced with
    /// the referenced entity's display value before writing the audit row.
    /// </summary>
    public AuditLookup? Lookup { get; }

    public AuditColumnConfig(string propertyName, string displayName, AuditLookup? lookup = null)
    {
        PropertyName = propertyName;
        DisplayName  = displayName;
        Lookup       = lookup;
    }
}
