using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class InAppNotification : BaseEntity
{
    public int                   UserId    { get; set; }
    public int?                  OrgId     { get; set; }
    public NotificationEventType EventType { get; set; }
    public string                Title     { get; set; } = string.Empty;
    public string                Body      { get; set; } = string.Empty;
    public bool                  IsRead    { get; set; } = false;
    public DateTime?             ReadAt    { get; set; }

    public User?         User         { get; init; }
    public Organization? Organization { get; init; }
}
