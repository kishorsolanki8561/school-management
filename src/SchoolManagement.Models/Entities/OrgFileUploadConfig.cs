namespace SchoolManagement.Models.Entities;

public sealed class OrgFileUploadConfig : BaseEntity
{
    public int    OrgId             { get; set; }
    public int    PageId            { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;  // comma-separated: ".pdf,.jpg,.png"
    public string AllowedMimeTypes  { get; set; } = string.Empty;  // comma-separated: "image/jpeg,..."
    public long   MaxFileSizeBytes  { get; set; }
    public bool   AllowMultiple     { get; set; }
    public bool   IsActive          { get; set; } = true;

    public Organization Organization { get; init; } = null!;
    public PageMaster   Page         { get; init; } = null!;
}
