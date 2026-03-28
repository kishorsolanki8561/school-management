namespace SchoolManagement.Models.DTOs;

/// <summary>
/// A single audit log entry within a batch tree.
/// Children are nested via <see cref="ParentAuditLogId"/> → <see cref="Children"/>.
/// </summary>
public sealed class AuditLogNodeResponse
{
    public int      Id         { get; init; }
    public string   EntityName { get; init; } = string.Empty;
    public string   EntityId   { get; init; } = string.Empty;
    public string   Action     { get; init; } = string.Empty;
    public string?  OldData    { get; init; }
    public string?  NewData    { get; init; }
    public string?  ModifiedBy { get; init; }
    public string?  CreatedBy  { get; init; }
    public string?  ScreenName { get; init; }
    public string   TableName  { get; init; } = string.Empty;
    public DateTime Timestamp  { get; init; }
    public string?  IpAddress  { get; init; }
    public string?  Location   { get; init; }

    /// <summary>Child audit entries nested under this node (e.g. PageMasterModules under PageMaster).</summary>
    public IList<AuditLogNodeResponse> Children { get; init; } = new List<AuditLogNodeResponse>();
}

/// <summary>
/// Groups all audit log entries that were saved in the same DB transaction (same BatchId).
/// <see cref="Entries"/> contains only the root-level nodes; each node's children are nested recursively.
/// </summary>
public sealed class AuditLogBatchResponse
{
    public string?  BatchId    { get; init; }

    /// <summary>Earliest timestamp of any entry in this batch.</summary>
    public DateTime Timestamp  { get; init; }

    public string?  ScreenName { get; init; }
    public string?  CreatedBy  { get; init; }

    /// <summary>Null when the batch contains only Created entries.</summary>
    public string?  ModifiedBy { get; init; }

    public string?  IpAddress  { get; init; }
    public string?  Location   { get; init; }

    /// <summary>Root-level audit nodes. Children are nested inside each node.</summary>
    public IList<AuditLogNodeResponse> Entries { get; init; } = new List<AuditLogNodeResponse>();
}
