using System.Text.Json.Serialization;

namespace SchoolManagement.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationChannel
{
    Email = 1,
    SMS   = 2,
    Push  = 3,
    InApp = 4,
}
