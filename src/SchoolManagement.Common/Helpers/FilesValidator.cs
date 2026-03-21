namespace SchoolManagement.Common.Helpers;

public interface IFilesValidator
{
    bool IsValidExtension(string fileName, IEnumerable<string> allowedExtensions);
    bool IsValidSize(long fileSizeBytes, long maxSizeBytes);
    bool IsValidContentType(string contentType, IEnumerable<string> allowedMimeTypes);
    ValidationResult Validate(string fileName, long fileSizeBytes, string contentType,
        IEnumerable<string> allowedExtensions, IEnumerable<string> allowedMimeTypes, long maxSizeBytes);
}

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Success() => new(true, Array.Empty<string>());
    public static ValidationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}

public sealed class FilesValidator : IFilesValidator
{
    public bool IsValidExtension(string fileName, IEnumerable<string> allowedExtensions)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return allowedExtensions.Select(e => e.TrimStart('.').ToLowerInvariant()).Contains(ext);
    }

    public bool IsValidSize(long fileSizeBytes, long maxSizeBytes) =>
        fileSizeBytes > 0 && fileSizeBytes <= maxSizeBytes;

    public bool IsValidContentType(string contentType, IEnumerable<string> allowedMimeTypes) =>
        allowedMimeTypes.Contains(contentType.ToLowerInvariant());

    public ValidationResult Validate(string fileName, long fileSizeBytes, string contentType,
        IEnumerable<string> allowedExtensions, IEnumerable<string> allowedMimeTypes, long maxSizeBytes)
    {
        var errors = new List<string>();

        if (!IsValidExtension(fileName, allowedExtensions))
            errors.Add($"File extension '{Path.GetExtension(fileName)}' is not allowed.");

        if (!IsValidSize(fileSizeBytes, maxSizeBytes))
            errors.Add($"File size {fileSizeBytes} bytes exceeds maximum of {maxSizeBytes} bytes.");

        if (!IsValidContentType(contentType, allowedMimeTypes))
            errors.Add($"Content type '{contentType}' is not allowed.");

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }
}
