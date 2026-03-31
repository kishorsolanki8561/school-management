using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

public sealed record SaveNotificationTemplateRequest(
    NotificationEventType  EventType,
    NotificationChannel?   Channel,    // null = generic (all channels fallback)
    string                 Subject,
    string                 Body,
    bool                   IsBodyHtml,
    string?                ToAddresses,
    string?                CcAddresses,
    string?                BccAddresses
);

public sealed record NotificationTemplateResponse(
    int                    Id,
    int?                   OrgId,
    NotificationEventType  EventType,
    NotificationChannel?   Channel,
    string                 Subject,
    string                 Body,
    bool                   IsBodyHtml,
    string?                ToAddresses,
    string?                CcAddresses,
    string?                BccAddresses,
    bool                   IsActive
);
