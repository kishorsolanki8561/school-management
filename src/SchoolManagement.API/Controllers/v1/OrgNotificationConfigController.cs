using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/org-notification-config")]
[Produces("application/json")]
[Authorize]
public sealed class OrgNotificationConfigController : ControllerBase
{
    private readonly INotificationConfigService _service;
    private readonly IRequestContext            _requestContext;

    public OrgNotificationConfigController(INotificationConfigService service, IRequestContext requestContext)
    {
        _service        = service;
        _requestContext = requestContext;
    }

    /// <summary>Create or update a channel config for the caller's org (upsert).</summary>
    [HttpPut]
    [SwaggerOperation(Summary = "Save Org Notification Config", Tags = new[] { "OrgNotificationConfig" })]
    [ProducesResponseType(typeof(ApiResponse<OrgNotificationConfigResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save([FromBody] SaveOrgNotificationConfigRequest request, CancellationToken cancellationToken)
    {
        var orgId = ResolveOrgId();
        var result = await _service.SaveAsync(orgId, request, cancellationToken);
        return Ok(ApiResponse<OrgNotificationConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get all channel configs for an org.</summary>
    [HttpGet("{orgId:int}")]
    [SwaggerOperation(Summary = "Get All Configs for Org", Tags = new[] { "OrgNotificationConfig" })]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrgNotificationConfigResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int orgId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAllByOrgAsync(orgId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrgNotificationConfigResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a specific channel config for an org.</summary>
    [HttpGet("{orgId:int}/{channel}")]
    [SwaggerOperation(Summary = "Get Config by Channel", Tags = new[] { "OrgNotificationConfig" })]
    [ProducesResponseType(typeof(ApiResponse<OrgNotificationConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int orgId, NotificationChannel channel, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(orgId, channel, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(
                ErrorDetail.NotFound($"Config for org {orgId} channel {channel} not found."),
                HttpContext.TraceIdentifier));
        return Ok(ApiResponse<OrgNotificationConfigResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a channel config for an org.</summary>
    [HttpDelete("{orgId:int}/{channel}")]
    [SwaggerOperation(Summary = "Delete Config by Channel", Tags = new[] { "OrgNotificationConfig" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int orgId, NotificationChannel channel, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(orgId, channel, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    private int ResolveOrgId()
    {
        if (_requestContext.OrgId is null)
            throw new InvalidOperationException("OrgId is required to manage notification config.");
        return _requestContext.OrgId.Value;
    }
}
