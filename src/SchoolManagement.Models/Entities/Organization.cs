namespace SchoolManagement.Models.Entities;

public sealed class Organization : BaseEntity
{
    public string  Name       { get; set; } = string.Empty;
    public string? Address    { get; set; }
    public bool    IsActive   { get; set; } = true;

    /// <summary>Short unique code assigned on approval (e.g. "SNR-2026").</summary>
    public string? SchoolCode  { get; set; }

    /// <summary>Set to true by OwnerAdmin after reviewing the registration request.</summary>
    public bool    IsApproved  { get; set; } = false;

    public DateTime? ApprovedAt { get; set; }
    public string?   ApprovedBy { get; set; }

    public ICollection<UserOrganizationMapping> UserOrganizationMappings { get; init; } = new List<UserOrganizationMapping>();
    public ICollection<SchoolApprovalRequest>   ApprovalRequests         { get; init; } = new List<SchoolApprovalRequest>();
}
