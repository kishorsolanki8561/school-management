using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Models.DTOs.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
