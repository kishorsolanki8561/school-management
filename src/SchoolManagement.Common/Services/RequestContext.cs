namespace SchoolManagement.Common.Services;

public sealed class RequestContext : IRequestContext
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public string? TraceId { get; set; }
}
