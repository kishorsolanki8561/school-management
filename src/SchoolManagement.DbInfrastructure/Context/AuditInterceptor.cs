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

    // Maps (EntityType, EntityId) → AuditLog.Id for all entities audited in
    // previous SaveChangesAsync calls within the same request/scope.
    // Enables ParentAuditLogId resolution across multiple SaveChangesAsync calls
    // (e.g. page saved first, modules saved next — all inside one transaction).
    private readonly Dictionary<(Type, int), int> _savedAuditIds = new();

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

        // Build all log objects keyed by their PendingAuditEntry.
        // ParentAuditLogId is left null here — resolved in two passes below.
        var entryToLog = new Dictionary<PendingAuditEntry, AuditLog>(ReferenceEqualityComparer.Instance);

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

            entryToLog[p] = new AuditLog
            {
                EntityName = p.Entity.GetType().Name,
                EntityId   = p.Entity.Id.ToString(),
                Action     = p.Action,
                OldData    = AuditValueHelper.Serialize(oldData),
                NewData    = AuditValueHelper.Serialize(newData),
                TableName  = p.TableName,
                BatchId    = p.BatchId,
                ScreenName = _requestContext.ScreenName,
                ModifiedBy = _requestContext.UserId ?? "System",
                CreatedBy  = _requestContext.Username,
                IpAddress  = _requestContext.IpAddress,
                Location   = _requestContext.Location,
                // ParentAuditLogId resolved below after parent logs are saved
            };
        }

        _pending.Clear();

        // Split into root logs (no parent entity) and child logs (have a parent entity).
        // "Parent entity" = the entity at the other end of a FK, e.g. PageMaster is
        // the parent of PageMasterModule.  Determined generically via EF FK metadata
        // in FindParentEntity — no hardcoding of entity types required.
        var parentPairs = entryToLog.Where(kv => kv.Key.ParentEntity is null).ToList();
        var childPairs  = entryToLog.Where(kv => kv.Key.ParentEntity is not null).ToList();

        // ── Pass 1: save root (parent) audit logs ─────────────────────────────
        // AuditLog does not extend BaseEntity — these saves will not re-trigger
        // the interceptor's audit cycle.
        if (parentPairs.Count > 0)
        {
            await eventData.Context.Set<AuditLog>()
                .AddRangeAsync(parentPairs.Select(kv => kv.Value), cancellationToken);
            await eventData.Context.SaveChangesAsync(cancellationToken);

            // Record the new AuditLog.Id so child entries (even from a LATER
            // SaveChangesAsync call in the same request) can resolve ParentAuditLogId.
            foreach (var (pending, log) in parentPairs)
                _savedAuditIds[(pending.Entity.GetType(), pending.Entity.Id)] = log.Id;
        }

        // ── Pass 2: resolve ParentAuditLogId, then save child audit logs ──────
        if (childPairs.Count > 0)
        {
            foreach (var (pending, childLog) in childPairs)
            {
                // Strategy A — same-batch parent: both parent and child were in this
                // SaveChangesAsync call; use the already-saved log's Id directly.
                if (pending.ParentPendingEntry is not null
                    && entryToLog.TryGetValue(pending.ParentPendingEntry, out var sameBatchLog))
                {
                    childLog.ParentAuditLogId = sameBatchLog.Id;
                }
                // Strategy B — cross-batch parent: parent was saved in a previous
                // SaveChangesAsync within this request (e.g. page saved first, modules
                // saved in the next call inside the same transaction).
                else if (pending.ParentEntity is not null
                    && _savedAuditIds.TryGetValue(
                        (pending.ParentEntity.GetType(), pending.ParentEntity.Id), out var crossBatchId))
                {
                    childLog.ParentAuditLogId = crossBatchId;
                }
                // If neither strategy resolves the parent, ParentAuditLogId stays null
                // (root-level log); the BatchId still groups the whole operation.
            }

            await eventData.Context.Set<AuditLog>()
                .AddRangeAsync(childPairs.Select(kv => kv.Value), cancellationToken);
            await eventData.Context.SaveChangesAsync(cancellationToken);

            // Record child log Ids too — they may be parents in deeper hierarchies.
            foreach (var (pending, log) in childPairs)
                _savedAuditIds[(pending.Entity.GetType(), pending.Entity.Id)] = log.Id;
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void CollectPendingEntries(DbContext context)
    {
        // Use the active transaction's ID so every SaveChangesAsync call within
        // the same transaction shares one BatchId — grouping the full parent-child
        // audit trail into a single queryable batch.
        var batchId = context.Database.CurrentTransaction?.TransactionId.ToString("N")
                      ?? Guid.NewGuid().ToString("N");

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
                    // Capture only effective columns (table-specific + defaults) that actually changed
                    foreach (var col in AuditValueHelper.GetEffectiveColumns(tableConfig))
                    {
                        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == col.PropertyName);
                        if (prop is null || !prop.IsModified) continue;

                        capturedOld.Add(new(col, prop.OriginalValue?.ToString()));
                        capturedNew.Add(new(col, prop.CurrentValue?.ToString()));
                    }
                    // Nothing changed on audited columns → skip
                    if (capturedOld.Count == 0) continue;

                    // Only persist the audit row when the change is meaningful:
                    //   • ModifiedBy is set  (a real user triggered the change)
                    //   • OR IsDeleted changed  (soft-delete / restore)
                    //   • OR IsActive changed   (activation / deactivation)
                    var modifiedByVal = entry.Properties
                        .FirstOrDefault(p => p.Metadata.Name == "ModifiedBy")
                        ?.CurrentValue as string;
                    var hasSensitiveChange = capturedOld.Any(c =>
                        c.Column.PropertyName is "IsDeleted" or "IsActive");

                    if (string.IsNullOrEmpty(modifiedByVal) && !hasSensitiveChange) continue;
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    foreach (var col in AuditValueHelper.GetEffectiveColumns(tableConfig))
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

        // Pass 2: link each child entry to its parent
        // • ParentEntity  — always stored (used for cross-SaveChanges lookup via _savedAuditIds)
        // • ParentPendingEntry — set only when the parent is also in THIS batch
        //   (used for same-SaveChanges parent-child linking without an extra DB lookup)
        foreach (var entry in changedEntries)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            if (!AuditConfiguration.Tables.ContainsKey(tableName)) continue;
            if (!entityToEntry.TryGetValue(entry.Entity, out var pending)) continue;

            var parentEntity = FindParentEntity(entry, context);
            if (parentEntity is null) continue;

            pending.ParentEntity = parentEntity;

            if (entityToEntry.TryGetValue(parentEntity, out var parentPending))
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
        var effectiveCols = AuditValueHelper.GetEffectiveColumns(config);
        var result        = new List<PendingColumnValue>(effectiveCols.Count);
        var type          = entity.GetType();

        foreach (var col in effectiveCols)
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
            string? display;

            if (cv.Column.Lookup is not null
                && int.TryParse(cv.RawValue, out var id)
                && id != 0)
            {
                display = await AuditValueHelper.ResolveLookupAsync(context, cv.Column.Lookup, id, cancellationToken);
            }
            else
            {
                display = AuditValueHelper.FormatValue(cv.Column, cv.RawValue);
            }

            if (AuditValueHelper.ShouldSkip(cv.Column, cv.RawValue, display)) continue;

            result[cv.Column.DisplayName] = display;
        }

        return result;
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

            var navName = fk.DependentToPrincipal?.Name;
            if (navName is not null)
            {
                // Path 1: EF Core navigation tracking state
                var nav = childEntry.Navigation(navName);
                if (nav.CurrentValue is BaseEntity parentViaNav)
                    return parentViaNav;

                // Path 2: direct CLR property read.
                // Handles init navigation properties (e.g. User = user set in object
                // initializer) where UserId is still 0 so the FK-value fallback
                // below cannot find the parent in the ChangeTracker.
                var clrProp = childEntry.Entity.GetType().GetProperty(navName);
                if (clrProp?.GetValue(childEntry.Entity) is BaseEntity parentViaClr)
                    return parentViaClr;
            }

            // Path 3: ChangeTracker lookup by FK value (for non-init / already-persisted parents).
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

        public BaseEntity         Entity             { get; }
        public string             Action             { get; }
        public string             TableName          { get; }
        public string             BatchId            { get; }
        public AuditTableConfig   TableConfig        { get; }

        /// <summary>The FK parent entity — set for any child entity regardless of batch.</summary>
        public BaseEntity?        ParentEntity       { get; set; }

        /// <summary>The parent's PendingAuditEntry — set only when parent is in the same SaveChanges batch.</summary>
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
