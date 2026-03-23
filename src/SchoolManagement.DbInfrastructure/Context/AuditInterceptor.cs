using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        // Step A: build AuditLog objects + a lookup so we can resolve ParentAuditLogId later
        var pendingToLog = new Dictionary<PendingAuditEntry, AuditLog>(
            ReferenceEqualityComparer.Instance);
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

            var log = new AuditLog
            {
                EntityName = p.Entity.GetType().Name,
                EntityId   = p.Entity.Id.ToString(),
                Action     = p.Action,
                OldData    = p.OldData,
                NewData    = newData,
                TableName  = p.TableName,
                BatchId    = p.BatchId,
                ScreenName = _requestContext.ScreenName,
                ModifiedBy = _requestContext.UserId ?? "System",
                CreatedBy  = _requestContext.Username,
                IpAddress  = _requestContext.IpAddress,
                Location   = _requestContext.Location,
            };

            logs.Add(log);
            pendingToLog[p] = log;
        }

        _pending.Clear();

        // Step B: insert audit rows — EF assigns real AuditLog.Id to each row
        // AuditLog does not extend BaseEntity so this save will not trigger another audit cycle
        await eventData.Context.Set<AuditLog>().AddRangeAsync(logs, cancellationToken);
        await eventData.Context.SaveChangesAsync(cancellationToken);

        // Step C: now that parent audit rows have real Ids, resolve ParentAuditLogId on children
        var needsUpdate = false;
        foreach (var (p, log) in pendingToLog)
        {
            if (p.ParentPendingEntry is not null &&
                pendingToLog.TryGetValue(p.ParentPendingEntry, out var parentLog))
            {
                log.ParentAuditLogId = parentLog.Id;
                needsUpdate = true;
            }
        }

        // Step D: persist parent links only when there were parent-child entries in this batch
        if (needsUpdate)
            await eventData.Context.SaveChangesAsync(cancellationToken);

        return result;
    }

    private void CollectPendingEntries(DbContext context)
    {
        var options  = new JsonSerializerOptions { WriteIndented = false };
        var batchId  = Guid.NewGuid().ToString();

        // Materialise the changed entries once to avoid repeated enumeration
        var changedEntries = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        // Pass 1: create PendingAuditEntry for every changed entity
        var entityToEntry = new Dictionary<BaseEntity, PendingAuditEntry>(
            ReferenceEqualityComparer.Instance);

        foreach (var entry in changedEntries)
        {
            string? oldData      = null;
            string? newData      = null;
            Dictionary<string, string?>? addedValues = null;
            string action;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    // Capture values — Id is wrong here (0 or temp), will be patched in SavedChangesAsync
                    addedValues = entry.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString());
                    break;

                case EntityState.Modified:
                    action = "Updated";
                    oldData = JsonSerializer.Serialize(
                        entry.OriginalValues.Properties
                            .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString()),
                        options);
                    newData = JsonSerializer.Serialize(
                        entry.CurrentValues.Properties
                            .ToDictionary(p => p.Name, p => entry.CurrentValues[p]?.ToString()),
                        options);
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    oldData = JsonSerializer.Serialize(
                        entry.OriginalValues.Properties
                            .ToDictionary(p => p.Name, p => entry.OriginalValues[p]?.ToString()),
                        options);
                    break;

                default:
                    continue;
            }

            var pending = new PendingAuditEntry(
                entity:    entry.Entity,
                action:    action,
                tableName: entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                batchId:   batchId)
            {
                OldData     = oldData,
                NewData     = newData,
                AddedValues = addedValues,
            };

            _pending.Add(pending);
            entityToEntry[entry.Entity] = pending;
        }

        // Pass 2: resolve immediate parent for each entity using EF FK metadata
        foreach (var entry in changedEntries)
        {
            if (!entityToEntry.TryGetValue(entry.Entity, out var pending))
                continue;

            var parentEntity = FindParentEntity(entry, context);
            if (parentEntity is not null &&
                entityToEntry.TryGetValue(parentEntity, out var parentPending))
            {
                pending.ParentPendingEntry = parentPending;
            }
            // If the parent was not changed in this same SaveChanges call,
            // leave ParentPendingEntry null — BatchId alone identifies the operation
        }
    }

    /// <summary>
    /// Generically finds the immediate BaseEntity parent of a child entity using EF Core FK metadata.
    /// Works for any number of hierarchy levels without hardcoding entity types.
    /// </summary>
    private static BaseEntity? FindParentEntity(EntityEntry<BaseEntity> childEntry, DbContext context)
    {
        foreach (var fk in childEntry.Metadata.GetForeignKeys())
        {
            var principalType = fk.PrincipalEntityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(principalType))
                continue;

            // Primary: use the navigation property reference.
            // This works even when the parent is Added (Id = 0) because object identity is unambiguous.
            var navName = fk.DependentToPrincipal?.Name;
            if (navName is not null)
            {
                var nav = childEntry.Navigation(navName);
                if (nav.CurrentValue is BaseEntity parentViaNav)
                    return parentViaNav;
            }

            // Fallback: find parent in ChangeTracker by type + FK value.
            // Used when only the FK int was set (navigation property not loaded).
            var fkProp = fk.Properties[0];
            if (childEntry.Property(fkProp.Name).CurrentValue is int parentId && parentId != 0)
            {
                return context.ChangeTracker
                    .Entries<BaseEntity>()
                    .FirstOrDefault(e => e.Metadata.ClrType == principalType && e.Entity.Id == parentId)
                    ?.Entity;
            }
        }

        return null;
    }

    private sealed class PendingAuditEntry
    {
        public PendingAuditEntry(BaseEntity entity, string action, string tableName, string batchId)
        {
            Entity    = entity;
            Action    = action;
            TableName = tableName;
            BatchId   = batchId;
        }

        public BaseEntity Entity { get; }
        public string Action { get; }
        public string TableName { get; }
        public string BatchId { get; }
        public PendingAuditEntry? ParentPendingEntry { get; set; }
        public string? OldData { get; init; }
        public string? NewData { get; init; }
        public Dictionary<string, string?>? AddedValues { get; init; }
    }
}
