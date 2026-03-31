namespace SchoolManagement.Services.Interfaces;

/// <summary>
/// Abstracts the real-time push (SignalR) so Services project does not
/// depend on the API project. The API project provides the implementation.
/// </summary>
public interface IRealtimeNotificationPusher
{
    Task PushAsync(int userId, object payload, CancellationToken ct = default);
}
