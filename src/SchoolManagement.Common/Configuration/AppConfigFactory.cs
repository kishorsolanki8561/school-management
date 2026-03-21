using Microsoft.Extensions.Configuration;

namespace SchoolManagement.Common.Configuration;

/// <summary>
/// Holds a static reference to the application's IConfiguration instance.
/// Initialized once at startup via <see cref="InitializeConfiguration.Initialize"/>.
/// </summary>
public static class AppConfigFactory
{
    public static IConfiguration? Configuration { get; private set; }

    public static void Initialize(IConfiguration configuration)
    {
        Configuration = configuration;
    }
}
