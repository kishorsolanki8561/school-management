using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations.Channels;

/// <summary>Firebase FCM push handler — stub for future implementation.</summary>
public sealed class PushChannelHandler : INotificationChannelHandler
{
    public NotificationChannel Channel => NotificationChannel.Push;

    public Task<ChannelResult> SendAsync(
        int                   orgId,
        NotificationTemplate? template,
        NotificationRequest   request,
        CancellationToken     ct = default)
    {
        // TODO: implement Firebase FCM in a future update
        return Task.FromResult(new ChannelResult(
            NotificationChannel.Push,
            false,
            "Push notifications (FCM) will be available in a future update."));
    }
}
