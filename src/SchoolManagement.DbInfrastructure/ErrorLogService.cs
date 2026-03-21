using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure;

public sealed class ErrorLogService : IErrorLogService
{
    private readonly SchoolManagementDbContext _context;

    public ErrorLogService(SchoolManagementDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Exception exception, string? requestPath = null, string? traceId = null, string? userId = null)
    {
        var log = new ErrorLog
        {
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Source = exception.Source,
            RequestPath = requestPath,
            TraceId = traceId,
            UserId = userId
        };

        await _context.ErrorLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
