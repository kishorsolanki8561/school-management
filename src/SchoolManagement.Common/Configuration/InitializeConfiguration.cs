using Microsoft.Extensions.Configuration;

namespace SchoolManagement.Common.Configuration;

/// <summary>
/// Initializes and exposes strongly-typed application settings.
/// Call <see cref="Initialize"/> once at application startup before any service resolution.
/// </summary>
public static class InitializeConfiguration
{
    public static JwtSettings JwtSettings { get; } = new();
    public static EncryptionSettings EncryptionSettings { get; } = new();
    public static DatabaseSettings DatabaseSettings { get; } = new();

    public static void Initialize(IConfiguration configuration)
    {
        AppConfigFactory.Initialize(configuration);
        configuration.GetSection(nameof(JwtSettings)).Bind(JwtSettings);
        configuration.GetSection(nameof(EncryptionSettings)).Bind(EncryptionSettings);
        configuration.GetSection(nameof(DatabaseSettings)).Bind(DatabaseSettings);
    }
}
