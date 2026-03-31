using Microsoft.AspNetCore.SignalR;
using SchoolManagement.API.Hubs;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.API.Services;

public sealed class SignalRNotificationPusher : IRealtimeNotificationPusher
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(int userId, object payload, CancellationToken ct = default)
        => _hub.Clients.User(userId.ToString())
               .SendAsync("ReceiveNotification", payload, ct);
}
