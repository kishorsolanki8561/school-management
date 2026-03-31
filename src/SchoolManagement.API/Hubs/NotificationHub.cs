using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SchoolManagement.API.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override Task OnConnectedAsync()  => base.OnConnectedAsync();
    public override Task OnDisconnectedAsync(Exception? exception) => base.OnDisconnectedAsync(exception);
}
