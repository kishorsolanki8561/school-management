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
public sealed class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>Create a new organization.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create Organization", Tags = new[] { "Master - Organization" })]
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await _organizationService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<OrganizationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing organization.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update Organization", Tags = new[] { "Master - Organization" })]
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await _organizationService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<OrganizationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete an organization.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete Organization", Tags = new[] { "Master - Organization" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get an organization by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Organization by Id", Tags = new[] { "Master - Organization" })]
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Organization {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<OrganizationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of organizations.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Organizations", Tags = new[] { "Master - Organization" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrganizationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrganizationResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
