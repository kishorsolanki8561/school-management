using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Helpers;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations.Channels;

public sealed class InAppChannelHandler : INotificationChannelHandler
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    private readonly SchoolManagementDbContext    _db;
    private readonly IRealtimeNotificationPusher  _pusher;

    public InAppChannelHandler(SchoolManagementDbContext db, IRealtimeNotificationPusher pusher)
    {
        _db     = db;
        _pusher = pusher;
    }

    public async Task<ChannelResult> SendAsync(
        int                   orgId,
        NotificationTemplate? template,
        NotificationRequest   request,
        CancellationToken     ct = default)
    {
        if (template is null)
            return Fail("In-app template not configured.");

        if (request.ToUserId is null)
            return Fail("No user ID provided for in-app notification.");

        var title = TemplatePlaceholder.Apply(template.Subject, request.Placeholders);
        var body  = TemplatePlaceholder.Apply(template.Body,    request.Placeholders);

        // Persist to DB
        var notification = new InAppNotification
        {
            UserId    = request.ToUserId.Value,
            OrgId     = orgId,
            EventType = request.EventType,
            Title     = title,
            Body      = body,
        };

        _db.InAppNotifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        // Push via SignalR (fire-and-forget — user may be offline)
        try
        {
            await _pusher.PushAsync(request.ToUserId.Value, new
            {
                notification.Id,
                notification.EventType,
                Title = title,
                Body  = body,
                notification.CreatedAt,
            }, ct);
        }
        catch
        {
            // User may be offline — notification is already persisted in DB
        }

        return Ok();
    }

    private static ChannelResult Ok()             => new(NotificationChannel.InApp, true,  null);
    private static ChannelResult Fail(string msg) => new(NotificationChannel.InApp, false, msg);
}
