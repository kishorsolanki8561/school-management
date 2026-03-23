using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Context;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContext _requestContext;
    private readonly List<PendingAuditEntry> _pending = new();

    public AuditInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    // Phase 1: capture entity data BEFORE save (Id not yet assigned for Added)
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CollectPendingEntries(eventData.Context);

        return ValueTask.FromResult(result);
    }

    // Phase 2: write audit logs AFTER save (real Ids now assigned by DB)
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0 || eventData.Context is null) return result;

        var options = new JsonSerializerOptions { WriteIndented = false };
        var logs = new List<AuditLog>(_pending.Count);

        foreach (var p in _pending)
        {
            string? newData = p.NewData;

            // For Added entities: patch the Id in the captured values dict now that the real Id is assigned
            if (p.AddedValues is not null)
            {
                p.AddedValues["Id"] = p.Entity.Id.ToString();
                newData = JsonSerializer.Serialize(p.AddedValues, options);
            }

            logs.Add(new AuditLog
            {
                EntityName = p.Entity.GetType().Name,
                EntityId   = p.Entity.Id.ToString(),
                Action     = p.Action,
                OldData    = p.OldData,
                NewData    = newData,
                TableName  = p.TableName,
                ScreenName = _requestContext.ScreenName,
                ModifiedBy = _requestContext.UserId ?? "System",
                CreatedBy  = _requestContext.Username,
                IpAddress  = _requestContext.IpAddress,
                Location   = _requestContext.Location,
            });
        }

        _pending.Clear();

        // AuditLog does not extend BaseEntity so this save will not trigger another audit cycle
        await eventData.Context.Set<AuditLog>().AddRangeAsync(logs, cancellationToken);
        await eventData.Context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private void CollectPendingEntries(DbContext context)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            string? oldData = null;
            string? newData = null;
            Dictionary<string, string?>? addedValues = null;
            string action;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    // Capture values — Id is wrong here, will be patched in SavedChangesAsync
                    addedValues = entry.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());
                    break;

                case EntityState.Modified:
                    action = "Updated";
                    oldData = JsonSerializer.Serialize(
                        entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString()),
                        options);
                    newData = JsonSerializer.Serialize(
                        entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString()),
                        options);
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

            _pending.Add(new PendingAuditEntry(
                entity:    entry.Entity,
                action:    action,
                tableName: entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name)
            {
                OldData     = oldData,
                NewData     = newData,
                AddedValues = addedValues,
            });
        }
    }

    private sealed class PendingAuditEntry
    {
        public PendingAuditEntry(BaseEntity entity, string action, string tableName)
        {
            Entity    = entity;
            Action    = action;
            TableName = tableName;
        }

        public BaseEntity Entity { get; }
        public string Action { get; }
        public string TableName { get; }
        public string? OldData { get; init; }
        public string? NewData { get; init; }
        public Dictionary<string, string?>? AddedValues { get; init; }
    }
}
