namespace SchoolManagement.Common.Services;

public sealed record ErrorLogEntry(
    Exception Exception,
    string? RequestPath     = null,
    string? TraceId         = null,
    string? UserId          = null,
    string? IpAddress       = null,
    string? Location        = null,
    string? HttpMethod      = null,
    int?    StatusCode      = null,
    string? RequestPayload  = null,
    string? ResponsePayload = null
);
