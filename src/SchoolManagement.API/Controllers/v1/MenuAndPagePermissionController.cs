using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class MenuAndPagePermissionController : ControllerBase
{
    private readonly IMenuAndPagePermissionService _permissionService;

    public MenuAndPagePermissionController(IMenuAndPagePermissionService permissionService)
        => _permissionService = permissionService;

    /// <summary>Toggle the IsAllowed flag on a permission row.</summary>
    [HttpPut("{id:int}/{roleId:int}")]
    [SwaggerOperation(Summary = "Toggle Permission", Tags = new[] { "Master - Permission" })]
    [ProducesResponseType(typeof(ApiResponse<MenuAndPagePermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, int roleId, CancellationToken cancellationToken)
    {
        var result = await _permissionService.UpdateAsync(id, roleId, cancellationToken);
        return Ok(ApiResponse<MenuAndPagePermissionResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a permission row by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Permission by Id", Tags = new[] { "Master - Permission" })]
    [ProducesResponseType(typeof(ApiResponse<MenuAndPagePermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Permission {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<MenuAndPagePermissionResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of permissions. Filter by menuId, pageId, roleId.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Permissions", Tags = new[] { "Master - Permission" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MenuAndPagePermissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, [FromQuery] int? menuId, [FromQuery] int? pageId, [FromQuery] int? roleId, CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetAllAsync(pagination, menuId, pageId, roleId, cancellationToken);
        return Ok(ApiResponse<PagedResult<MenuAndPagePermissionResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
