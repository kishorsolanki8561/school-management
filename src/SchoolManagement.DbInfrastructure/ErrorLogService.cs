using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure;

public sealed class ErrorLogService : IErrorLogService
{
    private const int MaxPayloadBytes = 65_536; // 64 KB

    private readonly SchoolManagementDbContext _context;

    public ErrorLogService(SchoolManagementDbContext context)
        => _context = context;

    public async Task LogAsync(ErrorLogEntry entry)
    {
        var log = new ErrorLog
        {
            Message         = entry.Exception.Message,
            StackTrace      = entry.Exception.StackTrace,
            Source          = entry.Exception.Source,
            RequestPath     = entry.RequestPath,
            TraceId         = entry.TraceId,
            UserId          = entry.UserId,
            IpAddress       = entry.IpAddress,
            Location        = entry.Location,
            HttpMethod      = entry.HttpMethod,
            StatusCode      = entry.StatusCode,
            RequestPayload  = Truncate(entry.RequestPayload),
            ResponsePayload = Truncate(entry.ResponsePayload),
        };

        await _context.ErrorLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    /// <summary>Caps payload to 64 KB before persisting to avoid bloating the DB.</summary>
    private static string? Truncate(string? value)
    {
        if (value is null) return null;
        if (System.Text.Encoding.UTF8.GetByteCount(value) <= MaxPayloadBytes) return value;
        var charLimit = MaxPayloadBytes / 4; // conservative: handles worst-case 4-byte UTF-8 chars
        return value[..Math.Min(charLimit, value.Length)] + "…[truncated]";
    }
}
