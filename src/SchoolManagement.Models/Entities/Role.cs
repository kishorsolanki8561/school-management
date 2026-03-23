namespace SchoolManagement.Models.Entities;

public sealed class Role : BaseEntity
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool    IsOrgRole   { get; set; }
    public ICollection<UserRoleMapping> UserRoleMappings { get; init; } = new List<UserRoleMapping>();
}
