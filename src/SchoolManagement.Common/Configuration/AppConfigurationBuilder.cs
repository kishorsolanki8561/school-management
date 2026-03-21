using Microsoft.Extensions.Configuration;

namespace SchoolManagement.Common.Configuration;

public static class AppConfigurationBuilder
{
    public static IConfiguration Build(string basePath, string environment = "Production")
    {
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public static T GetSection<T>(IConfiguration configuration, string sectionName) where T : new()
    {
        var section = new T();
        configuration.GetSection(sectionName).Bind(section);
        return section;
    }
}
