using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class OrgFileUploadConfigController : ControllerBase
{
    private readonly IOrgFileUploadConfigService _configService;

    public OrgFileUploadConfigController(IOrgFileUploadConfigService configService)
        => _configService = configService;

    /// <summary>Create a file upload config for a specific org + screen.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create Org File Upload Config", Tags = new[] { "File Upload" })]
    [ProducesResponseType(typeof(ApiResponse<OrgFileUploadConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrgFileUploadConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _configService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<OrgFileUploadConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing file upload config.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update Org File Upload Config", Tags = new[] { "File Upload" })]
    [ProducesResponseType(typeof(ApiResponse<OrgFileUploadConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateOrgFileUploadConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _configService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<OrgFileUploadConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a file upload config by ID.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Org File Upload Config by Id", Tags = new[] { "File Upload" })]
    [ProducesResponseType(typeof(ApiResponse<OrgFileUploadConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _configService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(
                ErrorDetail.NotFound($"File upload config {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<OrgFileUploadConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get the active file upload config for a specific org and screen.</summary>
    [HttpGet("screen")]
    [SwaggerOperation(Summary = "Get Org File Upload Config by Screen", Tags = new[] { "File Upload" })]
    [ProducesResponseType(typeof(ApiResponse<OrgFileUploadConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByScreen(
        [FromQuery] int orgId, [FromQuery] int pageId, CancellationToken cancellationToken)
    {
        var result = await _configService.GetByScreenAsync(orgId, pageId, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(
                ErrorDetail.NotFound($"No active config for org {orgId} / page {pageId}."),
                HttpContext.TraceIdentifier));

        return Ok(ApiResponse<OrgFileUploadConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
