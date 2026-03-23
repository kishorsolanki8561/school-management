namespace SchoolManagement.Models.Entities;

public sealed class Organization : BaseEntity
{
    public string  Name     { get; set; } = string.Empty;
    public string? Address  { get; set; }
    public bool    IsActive { get; set; } = true;
    public ICollection<UserOrganizationMapping> UserOrganizationMappings { get; init; } = new List<UserOrganizationMapping>();
}
