using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.DTOs.Auth;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Authenticate with username and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Login", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Register", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Exchange a refresh token for new tokens.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Refresh Token", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshTokenAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(Summary = "Logout", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] string refreshToken, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(refreshToken, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Request a password reset email.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Forgot Password", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<string>.Ok(SchoolManagement.Common.Constants.AppMessages.Auth.PasswordResetEmailSent, HttpContext.TraceIdentifier));
    }

    /// <summary>Reset password using a valid reset token.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Reset Password", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<string>.Ok(SchoolManagement.Common.Constants.AppMessages.Auth.PasswordResetSuccess, HttpContext.TraceIdentifier));
    }

    /// <summary>Switch the caller's active school context and receive a fresh token pair.</summary>
    [HttpPost("switch-school")]
    [Authorize]
    [SwaggerOperation(Summary = "Switch School", Tags = new[] { "Auth" })]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SwitchSchool([FromBody] SwitchSchoolRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? throw new UnauthorizedAccessException();

        var userId = int.Parse(userIdClaim);
        var result = await _authService.SwitchSchoolAsync(userId, request.OrgId, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
