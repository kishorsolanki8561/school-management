using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-management")]
[Produces("application/json")]
[Authorize]
public sealed class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>Create a new user in the caller's organisation.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create User", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update a user's basic info.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update User", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a user.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete User", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _userManagementService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a user by ID with their org-scoped roles.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get User by Id", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"User {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of users (scoped to caller's org, or all for OwnerAdmin).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Users", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Assign a role to a user within the caller's organisation.</summary>
    [HttpPost("{id:int}/roles")]
    [SwaggerOperation(Summary = "Assign Role", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.AssignRoleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Remove a role from a user within the caller's organisation.</summary>
    [HttpDelete("{id:int}/roles/{roleId:int}")]
    [SwaggerOperation(Summary = "Remove Role", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(int id, int roleId, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.RemoveRoleAsync(id, roleId, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Upgrade or downgrade a user between SuperAdmin and Admin (OwnerAdmin only).</summary>
    [HttpPut("{id:int}/role-level")]
    [SwaggerOperation(Summary = "Change Role Level", Tags = new[] { "User Management" })]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRoleLevel(int id, [FromBody] ChangeRoleLevelRequest request, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.ChangeRoleLevelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
