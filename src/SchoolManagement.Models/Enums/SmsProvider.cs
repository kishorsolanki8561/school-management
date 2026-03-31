using System.Text.Json.Serialization;

namespace SchoolManagement.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SmsProvider
{
    Twilio      = 1,
    Infobip     = 2,
    SslWireless = 3,
}
