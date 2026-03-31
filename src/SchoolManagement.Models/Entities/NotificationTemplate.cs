using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

/// <summary>
/// Mail / notification template.
/// OrgId  = null  → global default (created by OwnerAdmin).
/// Channel = null → generic template used as fallback for any channel.
///
/// Resolution order per channel:
///   1. OrgId=X  + Channel=specific
///   2. OrgId=X  + Channel=null   (org generic)
///   3. OrgId=null + Channel=specific (global channel-specific)
///   4. OrgId=null + Channel=null   (global generic)
///   5. Not found → warning
/// </summary>
public sealed class NotificationTemplate : BaseEntity
{
    public int?                   OrgId      { get; set; }
    public NotificationEventType  EventType  { get; set; }
    public NotificationChannel?   Channel    { get; set; }  // null = generic

    public string  Subject    { get; set; } = string.Empty;
    public string  Body       { get; set; } = string.Empty;
    public bool    IsBodyHtml { get; set; } = true;

    // Email extras
    public string? ToAddresses  { get; set; }
    public string? CcAddresses  { get; set; }
    public string? BccAddresses { get; set; }

    public bool IsActive { get; set; } = true;

    public Organization? Organization { get; init; }
}
