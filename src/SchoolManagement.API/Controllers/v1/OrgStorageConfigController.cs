using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org-storage-config")]
[Produces("application/json")]
[Authorize]
public sealed class OrgStorageConfigController : ControllerBase
{
    private readonly IOrgStorageConfigService _service;

    public OrgStorageConfigController(IOrgStorageConfigService service)
    {
        _service = service;
    }

    /// <summary>Create or update the storage config for the caller's org.</summary>
    [HttpPut]
    [SwaggerOperation(Summary = "Save Org Storage Config", Tags = new[] { "OrgStorageConfig" })]
    [ProducesResponseType(typeof(ApiResponse<OrgStorageConfigResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save([FromBody] SaveOrgStorageConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveAsync(request, cancellationToken);
        return Ok(ApiResponse<OrgStorageConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get the storage config for a specific org.</summary>
    [HttpGet("{orgId:int}")]
    [SwaggerOperation(Summary = "Get Org Storage Config", Tags = new[] { "OrgStorageConfig" })]
    [ProducesResponseType(typeof(ApiResponse<OrgStorageConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int orgId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByOrgIdAsync(orgId, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Storage config for org {orgId} not found."), HttpContext.TraceIdentifier));
        return Ok(ApiResponse<OrgStorageConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete the storage config for a specific org.</summary>
    [HttpDelete("{orgId:int}")]
    [SwaggerOperation(Summary = "Delete Org Storage Config", Tags = new[] { "OrgStorageConfig" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int orgId, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(orgId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
