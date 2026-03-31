using System.Text.Json.Serialization;

namespace SchoolManagement.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationEventType
{
    SchoolApproved      = 1,
    SchoolRejected      = 2,
    UserRegistered      = 3,
    StudentEnrolled     = 4,
    FeeReminder         = 5,
    GeneralNotification = 6,
}
