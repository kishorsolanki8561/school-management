using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class InAppNotificationService : IInAppNotificationService
{
    private readonly SchoolManagementDbContext _db;

    public InAppNotificationService(SchoolManagementDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<InAppNotificationResponse>> GetForUserAsync(
        int userId, bool? unreadOnly, PaginationRequest request, CancellationToken ct = default)
    {
        var query = _db.InAppNotifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (unreadOnly == true)
            query = query.Where(x => !x.IsRead);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .Select(x => new InAppNotificationResponse(
                x.Id, x.UserId, x.OrgId, x.EventType,
                x.Title, x.Body, x.IsRead, x.ReadAt, x.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<InAppNotificationResponse>.Create(items, totalCount, request.Page, request.PageSize);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
        => _db.InAppNotifications
              .CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task MarkReadAsync(int userId, MarkReadRequest request, CancellationToken ct = default)
    {
        var notifications = await _db.InAppNotifications
            .Where(x => x.UserId == userId && request.NotificationIds.Contains(x.Id))
            .ToListAsync(ct);

        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        var notifications = await _db.InAppNotifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(ct);

        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}
