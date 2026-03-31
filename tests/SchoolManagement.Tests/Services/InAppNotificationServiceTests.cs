using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class InAppNotificationServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext    _context;
    private readonly InAppNotificationService     _sut;

    public InAppNotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _sut     = new InAppNotificationService(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── GetForUserAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetForUserAsync_MultipleNotifications_ReturnsPaged()
    {
        await SeedNotifications(userId: 1, count: 5);

        var result = await _sut.GetForUserAsync(
            userId: 1, unreadOnly: null,
            new PaginationRequest { Page = 1, PageSize = 3 });

        result.Items.Should().HaveCount(3);
        result.Total.Should().Be(5);
    }

    [Fact]
    public async Task GetForUserAsync_UnreadOnlyTrue_ReturnsOnlyUnread()
    {
        // 3 unread, 2 read
        await SeedNotifications(userId: 2, count: 3, isRead: false);
        await SeedNotifications(userId: 2, count: 2, isRead: true);

        var result = await _sut.GetForUserAsync(
            userId: 2, unreadOnly: true,
            new PaginationRequest { Page = 1, PageSize = 20 });

        result.Items.Should().HaveCount(3);
        result.Items.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
    }

    [Fact]
    public async Task GetForUserAsync_OtherUserNotifications_NotIncluded()
    {
        await SeedNotifications(userId: 3, count: 4);
        await SeedNotifications(userId: 99, count: 10);

        var result = await _sut.GetForUserAsync(
            userId: 3, unreadOnly: null,
            new PaginationRequest { Page = 1, PageSize = 20 });

        result.Total.Should().Be(4);
        result.Items.Should().AllSatisfy(n => n.UserId.Should().Be(3));
    }

    // ── GetUnreadCountAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountAsync_MixedReadStatus_ReturnsCorrectCount()
    {
        await SeedNotifications(userId: 4, count: 3, isRead: false);
        await SeedNotifications(userId: 4, count: 2, isRead: true);

        var count = await _sut.GetUnreadCountAsync(userId: 4);

        count.Should().Be(3);
    }

    // ── MarkReadAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_ValidIds_MarksAsReadWithTimestamp()
    {
        await SeedNotifications(userId: 5, count: 3, isRead: false);
        var ids = await _context.InAppNotifications
            .Where(x => x.UserId == 5)
            .Select(x => x.Id)
            .Take(2)
            .ToListAsync();

        var before = DateTime.UtcNow;
        await _sut.MarkReadAsync(userId: 5, new MarkReadRequest(ids));

        var marked = await _context.InAppNotifications
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        marked.Should().AllSatisfy(n =>
        {
            n.IsRead.Should().BeTrue();
            n.ReadAt.Should().NotBeNull();
            n.ReadAt!.Value.Should().BeOnOrAfter(before);
        });

        var unread = await _context.InAppNotifications
            .CountAsync(x => x.UserId == 5 && !x.IsRead);
        unread.Should().Be(1); // third notification still unread
    }

    // ── MarkAllReadAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllReadAsync_UnreadNotifications_AllMarkedRead()
    {
        await SeedNotifications(userId: 6, count: 4, isRead: false);
        await SeedNotifications(userId: 6, count: 1, isRead: true);

        await _sut.MarkAllReadAsync(userId: 6);

        var unread = await _context.InAppNotifications
            .CountAsync(x => x.UserId == 6 && !x.IsRead);
        unread.Should().Be(0);

        var all = await _context.InAppNotifications
            .Where(x => x.UserId == 6)
            .ToListAsync();
        all.Should().HaveCount(5);
        all.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SeedNotifications(int userId, int count, bool isRead = false)
    {
        for (var i = 0; i < count; i++)
        {
            _context.InAppNotifications.Add(new()
            {
                UserId    = userId,
                OrgId     = 1,
                EventType = NotificationEventType.GeneralNotification,
                Title     = $"Notification {i + 1}",
                Body      = $"Body {i + 1}",
                IsRead    = isRead,
                ReadAt    = isRead ? DateTime.UtcNow : null
            });
        }

        await _context.SaveChangesAsync();
    }
}
