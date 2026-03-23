namespace SchoolManagement.Models.Entities;

public sealed class ErrorLog
{
    public int Id { get; set; }
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public string? Source { get; init; }
    public string? UserId { get; init; }
    public string? RequestPath { get; init; }
    public string? TraceId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public string? HttpMethod { get; init; }
    public int? StatusCode { get; init; }
    public string? RequestPayload { get; init; }
    public string? ResponsePayload { get; init; }
}
