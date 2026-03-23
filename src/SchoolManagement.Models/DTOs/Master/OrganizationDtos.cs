namespace SchoolManagement.Models.DTOs.Master;

public sealed class CreateOrganizationRequest
{
    public string  Name    { get; init; } = string.Empty;
    public string? Address { get; init; }
}

public sealed class UpdateOrganizationRequest
{
    public string  Name     { get; init; } = string.Empty;
    public string? Address  { get; init; }
    public bool    IsActive { get; init; } = true;
}

public sealed class OrganizationResponse
{
    public int      Id        { get; init; }
    public string   Name      { get; init; } = string.Empty;
    public string?  Address   { get; init; }
    public bool     IsActive  { get; init; }
    public DateTime CreatedAt { get; init; }
}
