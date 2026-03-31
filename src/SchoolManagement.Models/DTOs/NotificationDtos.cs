using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

/// <summary>Single notification dispatch request.</summary>
public sealed record NotificationRequest(
    int                        OrgId,
    NotificationEventType      EventType,
    Dictionary<string, string> Placeholders,

    // Targets — supply the relevant ones per channel
    string?  ToEmail,
    string?  ToPhone,
    int?     ToUserId,      // InApp + Push
    string?  DeviceToken,   // Push (FCM)

    /// <summary>Channels to send to. null = all enabled channels for this org.</summary>
    NotificationChannel[]? Channels = null
);

/// <summary>Result per channel after dispatch.</summary>
public sealed record ChannelResult(
    NotificationChannel Channel,
    bool                Success,
    string?             Message
);

/// <summary>Aggregated result for all channels.</summary>
public sealed record NotificationResult(IReadOnlyList<ChannelResult> Results);
