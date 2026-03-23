namespace SchoolManagement.Common.Services;

public interface IErrorLogService
{
    Task LogAsync(ErrorLogEntry entry);
}
