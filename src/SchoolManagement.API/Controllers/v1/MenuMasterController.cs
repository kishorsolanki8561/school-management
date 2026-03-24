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
public sealed class MenuMasterController : ControllerBase
{
    private readonly IMenuMasterService _menuService;

    public MenuMasterController(IMenuMasterService menuService)
        => _menuService = menuService;

    /// <summary>Create a new menu.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create Menu", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<MenuResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await _menuService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<MenuResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing menu.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update Menu", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<MenuResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await _menuService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<MenuResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a menu.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete Menu", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _menuService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a menu by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Menu by Id", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<MenuResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _menuService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Menu {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<MenuResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of menus.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Menus", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<MenuResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _menuService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<MenuResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get all permission details for a role with menu breadcrumb, page, module and action.</summary>
    [HttpGet("permission-details/{roleId:int}")]
    [SwaggerOperation(Summary = "Get Permission Details By Role", Tags = new[] { "Master - Menu" })]
    [ProducesResponseType(typeof(ApiResponse<IList<PermissionDetailResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionDetails(int roleId, CancellationToken cancellationToken)
    {
        var result = await _menuService.GetPermissionDetailsAsync(roleId, cancellationToken);
        return Ok(ApiResponse<IList<PermissionDetailResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
