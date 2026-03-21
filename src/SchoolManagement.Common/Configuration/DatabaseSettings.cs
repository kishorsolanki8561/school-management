namespace SchoolManagement.Common.Configuration;

public sealed class DatabaseSettings
{
    public int CommandTimeout { get; init; } = 30;
    public bool EnableSensitiveDataLogging { get; init; } = false;
}
