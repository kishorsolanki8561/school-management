using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Audit;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Repositories;

// ── Interface ─────────────────────────────────────────────────────────────────

/// <summary>
/// Wraps <see cref="IDapperHelper.ExecuteAsync"/> for write operations that need
/// an audit trail.  Inject this instead of <see cref="IDapperHelper"/> wherever
/// INSERT / UPDATE / DELETE queries must be audited.
/// </summary>
public interface IDapperAuditExecutor
{
    /// <summary>
    /// Executes a raw SQL write and — when <paramref name="audit"/> is supplied
    /// and the table is registered in <see cref="AuditConfiguration"/> — saves
    /// an <see cref="AuditLog"/> row automatically.
    /// </summary>
    Task<int> ExecuteAsync(
        string             sql,
        object?            param                = null,
        DapperAuditContext? audit               = null,
        CancellationToken  cancellationToken    = default);
}

// ── Implementation ────────────────────────────────────────────────────────────

public sealed class DapperAuditExecutor : IDapperAuditExecutor
{
    private readonly IDapperHelper             _dapper;
    private readonly SchoolManagementDbContext  _context;
    private readonly IRequestContext            _requestContext;

    public DapperAuditExecutor(
        IDapperHelper            dapper,
        SchoolManagementDbContext context,
        IRequestContext          requestContext)
    {
        _dapper         = dapper;
        _context        = context;
        _requestContext = requestContext;
    }

    public async Task<int> ExecuteAsync(
        string             sql,
        object?            param             = null,
        DapperAuditContext? audit            = null,
        CancellationToken  cancellationToken = default)
    {
        var rows = await _dapper.ExecuteAsync(sql, param);

        // Skip audit when: no context, nothing affected, or table not configured
        if (audit is null
            || rows == 0
            || !AuditConfiguration.Tables.TryGetValue(audit.TableName, out var tableConfig))
            return rows;

        // Build old / new value snapshots from the provided entity objects
        var oldDict = await AuditValueHelper.BuildFromEntityAsync(
            audit.OldEntity, tableConfig, _context, cancellationToken);

        var newDict = await AuditValueHelper.BuildFromEntityAsync(
            audit.NewEntity, tableConfig, _context, cancellationToken);

        // For "Updated": keep only columns where the value actually changed
        if (audit.Action == "Updated" && oldDict is not null && newDict is not null)
        {
            var changedOld = new Dictionary<string, string?>();
            var changedNew = new Dictionary<string, string?>();

            foreach (var key in newDict.Keys)
            {
                var oldVal = oldDict.GetValueOrDefault(key);
                var newVal = newDict[key];
                if (oldVal == newVal) continue;

                if (oldDict.ContainsKey(key)) changedOld[key] = oldVal;
                changedNew[key] = newVal;
            }

            // Also capture columns present in old but not in new (e.g. cleared fields)
            foreach (var key in oldDict.Keys.Except(newDict.Keys))
                changedOld[key] = oldDict[key];

            oldDict = changedOld.Count > 0 ? changedOld : null;
            newDict = changedNew.Count > 0 ? changedNew : null;

            // Nothing actually changed — skip audit
            if (oldDict is null && newDict is null) return rows;
        }

        var log = new AuditLog
        {
            EntityName = audit.TableName,
            EntityId   = audit.EntityId,
            Action     = audit.Action,
            TableName  = audit.TableName,
            OldData    = AuditValueHelper.Serialize(oldDict),
            NewData    = AuditValueHelper.Serialize(newDict),
            BatchId    = Guid.NewGuid().ToString(),
            ScreenName = _requestContext.ScreenName,
            ModifiedBy = _requestContext.UserId ?? "System",
            CreatedBy  = _requestContext.Username,
            IpAddress  = _requestContext.IpAddress,
            Location   = _requestContext.Location,
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return rows;
    }
}
