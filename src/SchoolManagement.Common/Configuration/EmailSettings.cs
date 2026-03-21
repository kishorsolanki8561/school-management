namespace SchoolManagement.Common.Configuration;

public sealed class EmailSettings
{
    public string SmtpHost               { get; init; } = string.Empty;
    public int    SmtpPort               { get; init; } = 587;
    public bool   UseSsl                 { get; init; } = false;
    public string Username               { get; init; } = string.Empty;
    public string Password               { get; init; } = string.Empty;
    public string FromEmail              { get; init; } = string.Empty;
    public string FromName               { get; init; } = "School Management";
    public string ResetPasswordBaseUrl   { get; init; } = string.Empty;
    public int    TokenExpirationHours   { get; init; } = 24;
}
