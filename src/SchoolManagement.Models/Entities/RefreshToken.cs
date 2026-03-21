namespace SchoolManagement.Models.Entities;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; init; } = string.Empty;
    public int UserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public User? User { get; init; }
}
