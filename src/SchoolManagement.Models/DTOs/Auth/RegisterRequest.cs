namespace SchoolManagement.Models.DTOs.Auth;

public sealed class RegisterRequest
{
    public string      Username { get; init; } = string.Empty;
    public string      Email    { get; init; } = string.Empty;
    public string      Password { get; init; } = string.Empty;
    public IList<int>? RoleIds  { get; init; }
    public IList<int>? OrgIds   { get; init; }
}
