namespace SchoolManagement.Common.Configuration;

public sealed class CorsSettings
{
    /// <summary>
    /// List of allowed UI origins. Example: ["http://localhost:4200", "https://app.yourdomain.com"]
    /// If empty, falls back to allow-any-origin (development convenience only).
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
