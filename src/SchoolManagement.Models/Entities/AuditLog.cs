namespace SchoolManagement.Models.Entities;

public sealed class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? OldData { get; init; }
    public string? NewData { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
    public string? CreatedBy { get; init; }
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public string? ScreenName { get; init; }
    public string TableName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
