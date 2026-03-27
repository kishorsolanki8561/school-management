namespace SchoolManagement.Common.Configuration;

/// <summary>
/// Default file upload constraints bound from the "FileUploadDefaults" appsettings section.
/// Applied when the caller is OwnerAdmin or when no org-specific config exists for the screen.
/// </summary>
public sealed class FileUploadDefaults
{
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public string[] AllowedMimeTypes  { get; set; } = Array.Empty<string>();
    public long     MaxFileSizeBytes  { get; set; } = 5_242_880; // 5 MB
    public bool     AllowMultiple     { get; set; } = false;
}
