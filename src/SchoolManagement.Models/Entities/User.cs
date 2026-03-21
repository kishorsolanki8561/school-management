using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class User : BaseEntity
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; set; } = true;
    public ICollection<RefreshToken> RefreshTokens { get; init; } = new List<RefreshToken>();
}
