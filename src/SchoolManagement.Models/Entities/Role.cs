namespace SchoolManagement.Models.Entities;

public sealed class Role : BaseEntity
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool    IsOrgRole   { get; set; }

    /// <summary>
    /// Null = system template role (mirrors UserRole enum, fixed IDs 1-29).
    /// Non-null = org-specific copy of a system role.
    /// </summary>
    public int?  OrgId        { get; set; }

    /// <summary>Points to the system role this was copied from. Null for system roles.</summary>
    public int?  SystemRoleId { get; set; }

    public Organization? Organization { get; init; }
    public Role?         SystemRole   { get; init; }

    public ICollection<UserRoleMapping>       UserRoleMappings { get; init; } = new List<UserRoleMapping>();
    public ICollection<MenuAndPagePermission> Permissions      { get; init; } = new List<MenuAndPagePermission>();
}
