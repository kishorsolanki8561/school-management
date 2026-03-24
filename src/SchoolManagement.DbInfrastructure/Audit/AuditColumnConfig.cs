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

    /// <summary>Text shown when a boolean property is <c>true</c>. Default: "Yes".</summary>
    public string BoolTrueDisplay  { get; }

    /// <summary>Text shown when a boolean property is <c>false</c>. Default: "No".</summary>
    public string BoolFalseDisplay { get; }

    public AuditColumnConfig(
        string       propertyName,
        string       displayName,
        AuditLookup? lookup           = null,
        string       boolTrueDisplay  = "Yes",
        string       boolFalseDisplay = "No")
    {
        PropertyName     = propertyName;
        DisplayName      = displayName;
        Lookup           = lookup;
        BoolTrueDisplay  = boolTrueDisplay;
        BoolFalseDisplay = boolFalseDisplay;
    }
}
