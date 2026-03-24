namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Resolves a raw FK integer value to a human-readable string at audit time.
/// Example: RoleId = 3  →  look up Role.Name  →  "Admin"
/// </summary>
public sealed class AuditLookup
{
    /// <summary>CLR type name of the referenced entity (e.g. "Role", "User", "Country").</summary>
    public string EntityTypeName { get; }

    /// <summary>Property on that entity whose value should be stored (e.g. "Name", "Username").</summary>
    public string ValueProperty { get; }

    public AuditLookup(string entityTypeName, string valueProperty)
    {
        EntityTypeName = entityTypeName;
        ValueProperty  = valueProperty;
    }
}
