namespace SchoolManagement.Common.Services;

public interface IRequestContext
{
    string? UserId { get; set; }
    string? Username { get; set; }
    string? Role { get; set; }
    string? IpAddress { get; set; }
    string? Location { get; set; }
    string? TraceId { get; set; }
}
