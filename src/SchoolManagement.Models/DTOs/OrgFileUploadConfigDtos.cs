namespace SchoolManagement.Models.DTOs;

public sealed class CreateOrgFileUploadConfigRequest
{
    public int    OrgId             { get; init; }
    public int    PageId            { get; init; }
    /// <summary>Comma-separated file extensions, e.g. ".pdf,.jpg,.png"</summary>
    public string AllowedExtensions { get; init; } = string.Empty;
    /// <summary>Comma-separated MIME types, e.g. "image/jpeg,application/pdf"</summary>
    public string AllowedMimeTypes  { get; init; } = string.Empty;
    public long   MaxFileSizeBytes  { get; init; }
    public bool   AllowMultiple     { get; init; }
}

public sealed class UpdateOrgFileUploadConfigRequest
{
    public string AllowedExtensions { get; init; } = string.Empty;
    public string AllowedMimeTypes  { get; init; } = string.Empty;
    public long   MaxFileSizeBytes  { get; init; }
    public bool   AllowMultiple     { get; init; }
    public bool   IsActive          { get; init; } = true;
}

public sealed class OrgFileUploadConfigResponse
{
    public int      Id                { get; init; }
    public int      OrgId             { get; init; }
    public int      PageId            { get; init; }
    public string   AllowedExtensions { get; init; } = string.Empty;
    public string   AllowedMimeTypes  { get; init; } = string.Empty;
    public long     MaxFileSizeBytes  { get; init; }
    public bool     AllowMultiple     { get; init; }
    public bool     IsActive          { get; init; }
    public DateTime CreatedAt         { get; init; }
}

public sealed class FileUploadResponse
{
    public string FileName    { get; init; } = string.Empty;
    public string FilePath    { get; init; } = string.Empty;
    public long   SizeBytes   { get; init; }
    public string ContentType { get; init; } = string.Empty;
}
