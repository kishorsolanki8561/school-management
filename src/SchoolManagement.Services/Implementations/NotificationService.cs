using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    private readonly SchoolManagementDbContext              _db;
    private readonly IEnumerable<INotificationChannelHandler> _handlers;

    public NotificationService(
        SchoolManagementDbContext                db,
        IEnumerable<INotificationChannelHandler> handlers)
    {
        _db       = db;
        _handlers = handlers;
    }

    public async Task<NotificationResult> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        // Determine which channels to send to
        NotificationChannel[] channels;
        if (request.Channels is { Length: > 0 })
        {
            channels = request.Channels;
        }
        else
        {
            // All enabled channels for this org
            var enabled = await _db.OrgNotificationConfigs
                .AsNoTracking()
                .Where(x => x.OrgId == request.OrgId && x.IsActive)
                .Select(x => x.Channel)
                .ToListAsync(ct);

            // InApp is always attempted (no external config needed)
            if (!enabled.Contains(NotificationChannel.InApp))
                enabled.Add(NotificationChannel.InApp);

            channels = enabled.ToArray();
        }

        // Dispatch all channels in parallel
        var tasks = channels.Select(channel => DispatchChannelAsync(channel, request, ct));
        var results = await Task.WhenAll(tasks);

        return new NotificationResult(results);
    }

    private async Task<ChannelResult> DispatchChannelAsync(
        NotificationChannel channel,
        NotificationRequest request,
        CancellationToken   ct)
    {
        var handler = _handlers.FirstOrDefault(h => h.Channel == channel);
        if (handler is null)
            return new ChannelResult(channel, false, $"No handler registered for channel {channel}.");

        var template = await ResolveTemplateAsync(request.OrgId, request.EventType, channel, ct);

        return await handler.SendAsync(request.OrgId, template, request, ct);
    }

    /// <summary>
    /// Template resolution order:
    ///   1. OrgId=X  + Channel=specific
    ///   2. OrgId=X  + Channel=null (org generic)
    ///   3. OrgId=null + Channel=specific (global channel-specific)
    ///   4. OrgId=null + Channel=null (global generic)
    ///   5. Returns null → channel handler returns warning
    /// </summary>
    private async Task<NotificationTemplate?> ResolveTemplateAsync(
        int                    orgId,
        NotificationEventType  eventType,
        NotificationChannel    channel,
        CancellationToken      ct)
    {
        // Load all candidate templates in one query
        var candidates = await _db.NotificationTemplates
            .AsNoTracking()
            .Where(t => t.IsActive
                     && t.EventType == eventType
                     && (t.OrgId == orgId || t.OrgId == null)
                     && (t.Channel == channel || t.Channel == null))
            .ToListAsync(ct);

        // Priority order
        return candidates.FirstOrDefault(t => t.OrgId == orgId  && t.Channel == channel)
            ?? candidates.FirstOrDefault(t => t.OrgId == orgId  && t.Channel == null)
            ?? candidates.FirstOrDefault(t => t.OrgId == null   && t.Channel == channel)
            ?? candidates.FirstOrDefault(t => t.OrgId == null   && t.Channel == null);
    }
}
