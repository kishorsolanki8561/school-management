namespace SchoolManagement.Common.Utilities;

public interface IIpLocationResolver
{
    Task<string?> ResolveAsync(string? ipAddress);
}

/// <summary>Stub implementation. Replace with a real IP-geolocation provider in production.</summary>
public sealed class IpLocationResolver : IIpLocationResolver
{
    public Task<string?> ResolveAsync(string? ipAddress)
    {
        // In production, call an IP geolocation API (e.g., ipapi.co, MaxMind GeoIP2)
        var location = ipAddress switch
        {
            null or "::1" or "127.0.0.1" => "Localhost",
            _ => "Unknown"
        };
        return Task.FromResult<string?>(location);
    }
}
