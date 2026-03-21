namespace SchoolManagement.Models.Common;

public sealed class ErrorDetail
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static ErrorDetail Create(string code, string message) =>
        new() { Code = code, Message = message };

    public static ErrorDetail NotFound(string message = "Resource not found") =>
        new() { Code = "NOT_FOUND", Message = message };

    public static ErrorDetail Unauthorized(string message = "Unauthorized") =>
        new() { Code = "UNAUTHORIZED", Message = message };

    public static ErrorDetail Validation(Dictionary<string, string[]> errors) =>
        new() { Code = "VALIDATION_ERROR", Message = "One or more validation errors occurred.", ValidationErrors = errors };

    public static ErrorDetail Internal(string message = "An internal error occurred") =>
        new() { Code = "INTERNAL_ERROR", Message = message };

    public static ErrorDetail Conflict(string message) =>
        new() { Code = "CONFLICT", Message = message };
}
