namespace SchoolManagement.Models.DTOs.Auth;

public sealed class LoginResponse
{
    public string   AccessToken       { get; init; } = string.Empty;
    public string   RefreshToken      { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public string   Username          { get; init; } = string.Empty;
    public string   Role              { get; init; } = string.Empty;

    /// <summary>Active school/tenant for this session. Null for OwnerAdmin (platform-wide).</summary>
    public int?    OrgId   { get; init; }
    public string? OrgName { get; init; }

    /// <summary>Role-scoped menu tree returned immediately after login.</summary>
    public IList<DynamicMenuResponse> Menus { get; init; } = new List<DynamicMenuResponse>();
}
