using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

public sealed record InAppNotificationResponse(
    int                    Id,
    int                    UserId,
    int?                   OrgId,
    NotificationEventType  EventType,
    string                 Title,
    string                 Body,
    bool                   IsRead,
    DateTime?              ReadAt,
    DateTime               CreatedAt
);

public sealed record MarkReadRequest(IReadOnlyList<int> NotificationIds);
