namespace SchoolManagement.DbInfrastructure.Audit;

/// <summary>
/// Central static audit registry.
///
/// Key   = EF Core table name (plural, e.g. "Users", "Roles").
/// Value = which columns to audit and how to display / resolve them.
///
/// RULE: If a table is NOT listed here, the AuditInterceptor will skip it entirely.
///       Add a table here only when you want a full, named-column audit trail.
/// </summary>
public static class AuditConfiguration
{
    /// <summary>
    /// BaseEntity audit fields automatically appended to every configured table's
    /// column list.  Override a default by adding the same PropertyName explicitly
    /// in the table's own column list — the explicit entry takes precedence.
    /// </summary>
    public static readonly IReadOnlyList<AuditColumnConfig> DefaultColumns = new AuditColumnConfig[]
    {
        new("CreatedAt",  "Created At"),
        new("CreatedBy",  "Created By"),
        new("ModifiedAt", "Modified At"),
        new("ModifiedBy", "Modified By"),
        new("IsDeleted",  "Deleted"),
        new("IsActive",   "Active"),
    };

    public static readonly IReadOnlyDictionary<string, AuditTableConfig> Tables =
        new Dictionary<string, AuditTableConfig>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Users ────────────────────────────────────────────────────────
            ["Users"] = new AuditTableConfig(
                new("Username", "User Name"),
                new("Email",    "Email"),
                new("IsAdmin",  "Is Admin")),

            // ── Roles ────────────────────────────────────────────────────────
            ["Roles"] = new AuditTableConfig(
                new("Name",        "Role Name"),
                new("Description", "Description"),
                new("IsOrgRole",   "Org Role")),

            // ── Organizations ─────────────────────────────────────────────────
            ["Organizations"] = new AuditTableConfig(
                new("Name",    "Organization Name"),
                new("Address", "Address")),

            // ── UserRoleMappings ──────────────────────────────────────────────
            ["UserRoleMappings"] = new AuditTableConfig(
                new AuditColumnConfig("RoleId", "Role", new AuditLookup("Role", "Name"))),

            // ── UserOrganizationMappings ──────────────────────────────────────
            ["UserOrganizationMappings"] = new AuditTableConfig(
                new AuditColumnConfig("OrgId", "Organization", new AuditLookup("Organization", "Name"))),

            // ── Countries ────────────────────────────────────────────────────
            ["Countries"] = new AuditTableConfig(
                new("Name", "Country Name"),
                new("Code", "Code")),

            // ── States ───────────────────────────────────────────────────────
            ["States"] = new AuditTableConfig(
                new("Name",      "State Name"),
                new("Code",      "Code"),
                new("CountryId", "Country", new AuditLookup("Country", "Name"))),

            // ── Cities ───────────────────────────────────────────────────────
            ["Cities"] = new AuditTableConfig(
                new("Name",    "City Name"),
                new("StateId", "State", new AuditLookup("State", "Name"))),
        };
}
