using Microsoft.Extensions.Configuration;
using SchoolManagement.Common.Constants;

namespace SchoolManagement.Common.Configuration;

/// <summary>
/// Initializes and exposes strongly-typed application settings.
/// Call <see cref="Initialize"/> once at application startup before any service resolution.
/// </summary>
public static class InitializeConfiguration
{
    public static ConnectionStrings  ConnectionStrings  { get; } = new();
    public static JwtSettings        JwtSettings        { get; } = new();
    public static EncryptionSettings EncryptionSettings { get; } = new();
    public static DatabaseSettings   DatabaseSettings   { get; } = new();
    public static EmailSettings      EmailSettings      { get; } = new();
    public static FileUploadDefaults FileUploadDefaults { get; } = new();
    public static CorsSettings       CorsSettings       { get; } = new();

    public static void Initialize(IConfiguration configuration)
    {
        AppConfigFactory.Initialize(configuration);
        configuration.GetSection(nameof(JwtSettings)).Bind(JwtSettings);
        configuration.GetSection(nameof(EncryptionSettings)).Bind(EncryptionSettings);
        configuration.GetSection(nameof(DatabaseSettings)).Bind(DatabaseSettings);
        configuration.GetSection(nameof(EmailSettings)).Bind(EmailSettings);
        configuration.GetSection(nameof(FileUploadDefaults)).Bind(FileUploadDefaults);
        configuration.GetSection(nameof(CorsSettings)).Bind(CorsSettings);
        configuration.GetSection(nameof(ConnectionStrings)).Bind(ConnectionStrings);

        if (string.IsNullOrWhiteSpace(ConnectionStrings.DefaultConnection))
            throw new InvalidOperationException(AppMessages.General.ConnectionStringMissing);
    }
}
