using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

/// <summary>
/// One row per (OrgId, Channel). Stores credentials for each notification channel.
/// OwnerAdmin falls back to appsettings — no DB row needed for platform defaults.
/// </summary>
public sealed class OrgNotificationConfig : BaseEntity
{
    public int                 OrgId    { get; set; }
    public NotificationChannel Channel  { get; set; }
    public bool                IsActive { get; set; } = true;

    // ── Email ─────────────────────────────────────────────────────────────────
    public string? SmtpHost    { get; set; }
    public int?    SmtpPort    { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? FromAddress  { get; set; }
    public string? FromName     { get; set; }
    public bool?   EnableSsl    { get; set; }

    // ── SMS ───────────────────────────────────────────────────────────────────
    public SmsProvider? SmsProvider   { get; set; }
    public string?      ApiKey        { get; set; }
    public string?      AccountSid    { get; set; }  // Twilio
    public string?      AuthToken     { get; set; }  // Twilio
    public string?      SenderNumber  { get; set; }
    public string?      SenderName    { get; set; }  // Infobip / SslWireless

    // ── Push (Firebase FCM — future) ──────────────────────────────────────────
    public string? PushServerKey { get; set; }
    public string? PushSenderId  { get; set; }

    public Organization? Organization { get; init; }
}
