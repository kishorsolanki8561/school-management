using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Context;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContext _requestContext;

    public AuditInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return result;

        var auditLogs = GenerateAuditLogs(eventData.Context);

        if (auditLogs.Count > 0)
            await eventData.Context.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);

        return result;
    }

    private List<AuditLog> GenerateAuditLogs(DbContext context)
    {
        var logs = new List<AuditLog>();
        var options = new JsonSerializerOptions { WriteIndented = false };

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            string? oldData = null;
            string? newData = null;
            string action;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    newData = JsonSerializer.Serialize(
                        entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString()),
                        options);
                    break;

                case EntityState.Modified:
                    action = "Updated";
                    var originalValues = entry.OriginalValues.Properties
                        .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString());
                    var currentValues = entry.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());
                    oldData = JsonSerializer.Serialize(originalValues, options);
                    newData = JsonSerializer.Serialize(currentValues, options);
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    oldData = JsonSerializer.Serialize(
                        entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString()),
                        options);
                    break;

                default:
                    continue;
            }

            logs.Add(new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Action = action,
                OldData = oldData,
                NewData = newData,
                ModifiedBy = _requestContext.UserId ?? "System",
                CreatedBy = _requestContext.Username,
                IpAddress = _requestContext.IpAddress,
                Location = _requestContext.Location,
                ScreenName = _requestContext.ScreenName,
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name
            });
        }

        return logs;
    }
}
