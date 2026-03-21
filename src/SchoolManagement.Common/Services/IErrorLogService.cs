namespace SchoolManagement.Common.Services;

public interface IErrorLogService
{
    Task LogAsync(Exception exception, string? requestPath = null, string? traceId = null, string? userId = null);
}
