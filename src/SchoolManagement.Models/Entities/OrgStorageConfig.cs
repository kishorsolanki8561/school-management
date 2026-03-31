using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class OrgStorageConfig : BaseEntity
{
    public int         OrgId        { get; set; }
    public StorageType StorageType  { get; set; }
    public bool        IsActive     { get; set; } = true;

    // ── Hosting Server ────────────────────────────────────────────────────────
    /// <summary>Base folder path on the hosting server (e.g. /var/uploads/schools).</summary>
    public string? BasePath { get; set; }

    // ── AWS S3 ────────────────────────────────────────────────────────────────
    public string? BucketName { get; set; }
    public string? Region     { get; set; }
    public string? AccessKey  { get; set; }
    public string? SecretKey  { get; set; }

    // ── Azure Blob ────────────────────────────────────────────────────────────
    public string? ContainerName     { get; set; }
    public string? ConnectionString  { get; set; }

    public Organization? Organization { get; init; }
}
