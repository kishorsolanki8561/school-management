using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Services.Interfaces;

/// <summary>Strategy interface — one implementation per NotificationChannel.</summary>
public interface INotificationChannelHandler
{
    NotificationChannel Channel { get; }

    Task<ChannelResult> SendAsync(
        int                        orgId,
        NotificationTemplate?      template,
        NotificationRequest        request,
        CancellationToken          ct = default);
}
