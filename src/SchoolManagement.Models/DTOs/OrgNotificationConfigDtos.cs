using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

public sealed record SaveOrgNotificationConfigRequest(
    NotificationChannel Channel,

    // Email
    string? SmtpHost,
    int?    SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    string? FromAddress,
    string? FromName,
    bool?   EnableSsl,

    // SMS
    SmsProvider? SmsProvider,
    string?      ApiKey,
    string?      AccountSid,
    string?      AuthToken,
    string?      SenderNumber,
    string?      SenderName,

    // Push (future FCM)
    string? PushServerKey,
    string? PushSenderId
);

public sealed record OrgNotificationConfigResponse(
    int                 Id,
    int                 OrgId,
    NotificationChannel Channel,
    bool                IsActive,

    // Email (password omitted)
    string? SmtpHost,
    int?    SmtpPort,
    string? SmtpUsername,
    string? FromAddress,
    string? FromName,
    bool?   EnableSsl,

    // SMS (secrets omitted)
    SmsProvider? SmsProvider,
    string?      SenderNumber,
    string?      SenderName,

    // Push (server key omitted)
    string? PushSenderId
);
