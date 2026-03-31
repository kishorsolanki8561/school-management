using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notification-template")]
[Produces("application/json")]
[Authorize]
public sealed class NotificationTemplateController : ControllerBase
{
    private readonly INotificationTemplateService _service;

    public NotificationTemplateController(INotificationTemplateService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create or update a notification template.
    /// OwnerAdmin → global default (OrgId = null).
    /// Org user → org-specific template.
    /// </summary>
    [HttpPut]
    [SwaggerOperation(Summary = "Save Notification Template", Tags = new[] { "NotificationTemplate" })]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Save([FromBody] SaveNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SaveAsync(request, cancellationToken);
        return Ok(ApiResponse<NotificationTemplateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get all templates for an org. Pass orgId=0 for global defaults.</summary>
    [HttpGet("{orgId:int}")]
    [SwaggerOperation(Summary = "Get All Templates for Org", Tags = new[] { "NotificationTemplate" })]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationTemplateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int orgId, CancellationToken cancellationToken)
    {
        int? resolvedOrgId = orgId == 0 ? null : orgId;
        var result = await _service.GetAllAsync(resolvedOrgId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationTemplateResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a specific template by org, event type, and channel.</summary>
    [HttpGet("{orgId:int}/{eventType}/{channel}")]
    [SwaggerOperation(Summary = "Get Template by Event + Channel", Tags = new[] { "NotificationTemplate" })]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        int orgId, NotificationEventType eventType, NotificationChannel channel, CancellationToken cancellationToken)
    {
        int? resolvedOrgId = orgId == 0 ? null : orgId;
        var result = await _service.GetAsync(resolvedOrgId, eventType, channel, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(
                ErrorDetail.NotFound($"Template not found for event {eventType} channel {channel}."),
                HttpContext.TraceIdentifier));
        return Ok(ApiResponse<NotificationTemplateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a template by id.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete Notification Template", Tags = new[] { "NotificationTemplate" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
