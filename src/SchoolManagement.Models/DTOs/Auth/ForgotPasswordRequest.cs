using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
