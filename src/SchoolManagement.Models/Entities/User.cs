namespace SchoolManagement.Models.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; init; } = new List<RefreshToken>();
    public ICollection<UserRoleMapping> UserRoleMappings { get; init; } = new List<UserRoleMapping>();
    public ICollection<UserOrganizationMapping> UserOrganizationMappings { get; init; } = new List<UserOrganizationMapping>();
}
