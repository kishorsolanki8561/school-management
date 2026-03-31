namespace SchoolManagement.Models.Entities;

public sealed class UserRoleMapping : BaseEntity
{
    public int   UserId { get; init; }
    public int   RoleId { get; init; }

    /// <summary>
    /// Org-scoped role assignment. Null = system-level role (OwnerAdmin, SuperAdmin).
    /// Non-null = role applies only within this organisation (Admin, Staff, etc.).
    /// </summary>
    public int?  OrgId  { get; set; }

    public User?         User         { get; init; }
    public Role?         Role         { get; init; }
    public Organization? Organization { get; init; }
}
