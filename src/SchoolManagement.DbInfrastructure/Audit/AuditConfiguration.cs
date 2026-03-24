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
    public static readonly IReadOnlyDictionary<string, AuditTableConfig> Tables =
        new Dictionary<string, AuditTableConfig>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Users ────────────────────────────────────────────────────────
            ["Users"] = new AuditTableConfig(
                new("Username",  "User Name"),
                new("Email",     "Email"),
                new("IsActive",  "Active"),
                new("IsAdmin",   "Is Admin"),
                new("IsDeleted", "Deleted")),

            // ── Roles ────────────────────────────────────────────────────────
            ["Roles"] = new AuditTableConfig(
                new("Name",        "Role Name"),
                new("Description", "Description"),
                new("IsOrgRole",   "Org Role"),
                new("IsDeleted",   "Deleted")),

            // ── Organizations ─────────────────────────────────────────────────
            ["Organizations"] = new AuditTableConfig(
                new("Name",      "Organization Name"),
                new("Address",   "Address"),
                new("IsActive",  "Active"),
                new("IsDeleted", "Deleted")),

            // ── UserRoleMappings ──────────────────────────────────────────────
            ["UserRoleMappings"] = new AuditTableConfig(
                new AuditColumnConfig("RoleId", "Role", new AuditLookup("Role", "Name"))),

            // ── UserOrganizationMappings ──────────────────────────────────────
            ["UserOrganizationMappings"] = new AuditTableConfig(
                new AuditColumnConfig("OrgId",  "Organization", new AuditLookup("Organization", "Name"))),

            // ── Countries ────────────────────────────────────────────────────
            ["Countries"] = new AuditTableConfig(
                new("Name",      "Country Name"),
                new("Code",      "Code"),
                new("IsActive",  "Active"),
                new("IsDeleted", "Deleted")),

            // ── States ───────────────────────────────────────────────────────
            ["States"] = new AuditTableConfig(
                new("Name",      "State Name"),
                new("Code",      "Code"),
                new("CountryId", "Country", new AuditLookup("Country", "Name")),
                new("IsActive",  "Active"),
                new("IsDeleted", "Deleted")),

            // ── Cities ───────────────────────────────────────────────────────
            ["Cities"] = new AuditTableConfig(
                new("Name",    "City Name"),
                new("StateId", "State",    new AuditLookup("State", "Name")),
                new("IsActive",  "Active"),
                new("IsDeleted", "Deleted")),
        };
}
