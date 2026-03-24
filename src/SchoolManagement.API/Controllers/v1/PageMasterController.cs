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
public sealed class PageMasterController : ControllerBase
{
    private readonly IPageMasterService _pageService;

    public PageMasterController(IPageMasterService pageService)
        => _pageService = pageService;

    /// <summary>Create a new page with its modules and actions (hierarchical).</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create Page", Tags = new[] { "Master - Page" })]
    [ProducesResponseType(typeof(ApiResponse<PageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await _pageService.CreatePageAsync(request, cancellationToken);
        return Ok(ApiResponse<PageResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update a page — hierarchical upsert of modules and actions.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update Page", Tags = new[] { "Master - Page" })]
    [ProducesResponseType(typeof(ApiResponse<PageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePage(int id, [FromBody] UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await _pageService.UpdatePageAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PageResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a page and all its modules, actions, and permissions.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete Page", Tags = new[] { "Master - Page" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePage(int id, CancellationToken cancellationToken)
    {
        await _pageService.DeletePageAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a page by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Page by Id", Tags = new[] { "Master - Page" })]
    [ProducesResponseType(typeof(ApiResponse<PageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageById(int id, CancellationToken cancellationToken)
    {
        var result = await _pageService.GetPageByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Page {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<PageResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of pages.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Pages", Tags = new[] { "Master - Page" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PageResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPages([FromQuery] PaginationRequest pagination, [FromQuery] int? menuId, CancellationToken cancellationToken)
    {
        var result = await _pageService.GetAllPagesAsync(pagination, menuId, cancellationToken);
        return Ok(ApiResponse<PagedResult<PageResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
