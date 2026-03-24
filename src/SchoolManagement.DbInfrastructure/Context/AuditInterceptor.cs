using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Audit;
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

    // ── Phase 1: capture BEFORE save ─────────────────────────────────────────
    // Added   → defer value reading; FK values may still be 0 (temp).
    // Modified → capture old/new values NOW (OriginalValues will be lost after save).
    // Deleted  → capture old values NOW.
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CollectPendingEntries(eventData.Context);

        return ValueTask.FromResult(result);
    }

    // ── Phase 2: write audit rows AFTER save ──────────────────────────────────
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0 || eventData.Context is null) return result;

        var options      = new JsonSerializerOptions { WriteIndented = false };
        var pendingToLog = new Dictionary<PendingAuditEntry, AuditLog>(ReferenceEqualityComparer.Instance);
        var logs         = new List<AuditLog>(_pending.Count);

        foreach (var p in _pending)
        {
            // For Added entries, read values NOW (after save) so FK fixup has run
            // and all navigation-based FK columns hold real DB IDs.
            var rawOld = p.CapturedOldValues;
            var rawNew = p.Action == "Created"
                ? ReadFromEntity(p.Entity, p.TableConfig)
                : p.CapturedNewValues;

            // Resolve FK lookups: e.g. RoleId=3 → "Admin"
            var oldData = await ResolveDictionaryAsync(rawOld, eventData.Context, cancellationToken);
            var newData = await ResolveDictionaryAsync(rawNew, eventData.Context, cancellationToken);

            var log = new AuditLog
            {
                EntityName = p.Entity.GetType().Name,
                EntityId   = p.Entity.Id.ToString(),   // real ID available after save
                Action     = p.Action,
                OldData    = oldData is { Count: > 0 } ? JsonSerializer.Serialize(oldData, options) : null,
                NewData    = newData is { Count: > 0 } ? JsonSerializer.Serialize(newData, options) : null,
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

        // AuditLog does not extend BaseEntity — this save will not trigger another audit cycle
        await eventData.Context.Set<AuditLog>().AddRangeAsync(logs, cancellationToken);
        await eventData.Context.SaveChangesAsync(cancellationToken);

        // Resolve ParentAuditLogId now that parent rows have real Ids
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

        if (needsUpdate)
            await eventData.Context.SaveChangesAsync(cancellationToken);

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void CollectPendingEntries(DbContext context)
    {
        var batchId = Guid.NewGuid().ToString();

        var changedEntries = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var entityToEntry = new Dictionary<BaseEntity, PendingAuditEntry>(ReferenceEqualityComparer.Instance);

        // Pass 1: build PendingAuditEntry per entity
        foreach (var entry in changedEntries)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;

            // Requirement 4: table not in config → skip silently
            if (!AuditConfiguration.Tables.TryGetValue(tableName, out var tableConfig))
                continue;

            List<PendingColumnValue> capturedOld = new();
            List<PendingColumnValue> capturedNew = new();
            string action;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    // Values deferred — read from entity in SavedChangesAsync after FK fixup
                    break;

                case EntityState.Modified:
                    action = "Updated";
                    // Capture only configured columns that actually changed
                    foreach (var col in tableConfig.Columns)
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == col.PropertyName);
                        if (prop is null || !prop.IsModified) continue;

                        capturedOld.Add(new(col, prop.OriginalValue?.ToString()));
                        capturedNew.Add(new(col, prop.CurrentValue?.ToString()));
                    }
                    // Nothing changed on audited columns → skip
                    if (capturedOld.Count == 0) continue;
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    foreach (var col in tableConfig.Columns)
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == col.PropertyName);
                        if (prop is null) continue;
                        capturedOld.Add(new(col, prop.OriginalValue?.ToString()));
                    }
                    break;

                default:
                    continue;
            }

            var pending = new PendingAuditEntry(entry.Entity, action, tableName, batchId, tableConfig)
            {
                CapturedOldValues = capturedOld,
                CapturedNewValues = capturedNew,
            };

            _pending.Add(pending);
            entityToEntry[entry.Entity] = pending;
        }

        // Pass 2: link each child entry to its parent entry (for ParentAuditLogId)
        foreach (var entry in changedEntries)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            if (!AuditConfiguration.Tables.ContainsKey(tableName)) continue;
            if (!entityToEntry.TryGetValue(entry.Entity, out var pending)) continue;

            var parentEntity = FindParentEntity(entry, context);
            if (parentEntity is not null && entityToEntry.TryGetValue(parentEntity, out var parentPending))
                pending.ParentPendingEntry = parentPending;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads all configured column values from the entity object directly (via reflection).
    /// Used for Added entries after save, when FK fixup has assigned real IDs.
    /// </summary>
    private static IReadOnlyList<PendingColumnValue> ReadFromEntity(BaseEntity entity, AuditTableConfig config)
    {
        var result = new List<PendingColumnValue>(config.Columns.Count);
        var type   = entity.GetType();

        foreach (var col in config.Columns)
        {
            var clrProp = type.GetProperty(col.PropertyName);
            if (clrProp is null) continue;
            result.Add(new(col, clrProp.GetValue(entity)?.ToString()));
        }

        return result;
    }

    /// <summary>
    /// Builds a display-name keyed dictionary, resolving any FK lookups.
    /// Returns null when the input list is empty (so OldData / NewData stays null).
    /// </summary>
    private static async Task<Dictionary<string, string?>?> ResolveDictionaryAsync(
        IReadOnlyList<PendingColumnValue> values,
        DbContext context,
        CancellationToken cancellationToken)
    {
        if (values.Count == 0) return null;

        var result = new Dictionary<string, string?>(values.Count);

        foreach (var cv in values)
        {
            string? display = cv.RawValue;

            if (cv.Column.Lookup is not null
                && int.TryParse(cv.RawValue, out var id)
                && id != 0)
            {
                display = await ResolveLookupAsync(context, cv.Column.Lookup, id, cancellationToken);
            }

            result[cv.Column.DisplayName] = display;
        }

        return result;
    }

    /// <summary>
    /// Resolves a FK integer to the display value of the referenced entity.
    /// Checks the EF identity map first (cheap) then falls back to a DB query.
    /// Returns the raw ID string if the entity or property cannot be found.
    /// </summary>
    private static async Task<string?> ResolveLookupAsync(
        DbContext context,
        AuditLookup lookup,
        int id,
        CancellationToken cancellationToken)
    {
        var entityType = context.Model
            .GetEntityTypes()
            .FirstOrDefault(e => e.ClrType.Name == lookup.EntityTypeName);

        if (entityType is null) return id.ToString();

        // FindAsync checks identity map first — no extra DB round-trip if entity is tracked
        var entity = await context.FindAsync(
            entityType.ClrType,
            new object?[] { id },
            cancellationToken);

        if (entity is null) return id.ToString();

        var valueProp = entityType.ClrType.GetProperty(lookup.ValueProperty);
        return valueProp?.GetValue(entity)?.ToString() ?? id.ToString();
    }

    /// <summary>
    /// Generically finds the immediate BaseEntity parent of a child using EF FK metadata.
    /// Works for any depth without hardcoding entity types.
    /// </summary>
    private static BaseEntity? FindParentEntity(EntityEntry<BaseEntity> childEntry, DbContext context)
    {
        foreach (var fk in childEntry.Metadata.GetForeignKeys())
        {
            var principalType = fk.PrincipalEntityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(principalType)) continue;

            // Primary: use the navigation property reference (works even when parent Id is still 0)
            var navName = fk.DependentToPrincipal?.Name;
            if (navName is not null)
            {
                var nav = childEntry.Navigation(navName);
                if (nav.CurrentValue is BaseEntity parentViaNav)
                    return parentViaNav;
            }

            // Fallback: find in ChangeTracker by type + FK value.
            // Only return if actually found — if this FK's principal is not in the
            // current batch, continue to the next FK rather than returning null early.
            var fkProp = fk.Properties[0];
            if (childEntry.Property(fkProp.Name).CurrentValue is int parentId && parentId != 0)
            {
                var found = context.ChangeTracker
                    .Entries<BaseEntity>()
                    .FirstOrDefault(e => e.Metadata.ClrType == principalType && e.Entity.Id == parentId)
                    ?.Entity;

                if (found is not null)
                    return found;
                // principal not in this batch — try the next FK
            }
        }

        return null;
    }

    // ── Private data types ────────────────────────────────────────────────────

    private sealed class PendingAuditEntry
    {
        public PendingAuditEntry(
            BaseEntity entity, string action, string tableName,
            string batchId, AuditTableConfig tableConfig)
        {
            Entity      = entity;
            Action      = action;
            TableName   = tableName;
            BatchId     = batchId;
            TableConfig = tableConfig;
        }

        public BaseEntity        Entity             { get; }
        public string            Action             { get; }
        public string            TableName          { get; }
        public string            BatchId            { get; }
        public AuditTableConfig  TableConfig        { get; }
        public PendingAuditEntry? ParentPendingEntry { get; set; }

        /// <summary>
        /// Pre-save captured values for Modified (old+new) and Deleted (old).
        /// Empty for Added — values are read post-save via ReadFromEntity.
        /// </summary>
        public IReadOnlyList<PendingColumnValue> CapturedOldValues { get; init; } = Array.Empty<PendingColumnValue>();
        public IReadOnlyList<PendingColumnValue> CapturedNewValues { get; init; } = Array.Empty<PendingColumnValue>();
    }

    private sealed record PendingColumnValue(AuditColumnConfig Column, string? RawValue);
}
