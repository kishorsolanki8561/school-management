namespace SchoolManagement.Models.Entities;

public sealed class PasswordResetToken
{
    public int      Id        { get; set; }
    public int      UserId    { get; init; }
    public string   Token     { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool     IsUsed    { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public User? User { get; init; }
}
