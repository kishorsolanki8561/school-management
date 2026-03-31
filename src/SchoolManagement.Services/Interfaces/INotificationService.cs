using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Dispatches a notification to one or more channels in parallel.
    /// If request.Channels is null, all enabled channels for the org are used.
    /// Each channel result is returned independently — one failure does not block others.
    /// </summary>
    Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken ct = default);
}
