using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/in-app-notification")]
[Produces("application/json")]
[Authorize]
public sealed class InAppNotificationController : ControllerBase
{
    private readonly IInAppNotificationService _service;
    private readonly IRequestContext           _requestContext;

    public InAppNotificationController(IInAppNotificationService service, IRequestContext requestContext)
    {
        _service        = service;
        _requestContext = requestContext;
    }

    /// <summary>Get paginated in-app notifications for the current user.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get My Notifications", Tags = new[] { "InAppNotification" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InAppNotificationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(_requestContext.UserId ?? "0");
        var result = await _service.GetForUserAsync(userId, unreadOnly, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<InAppNotificationResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get unread notification count for the current user.</summary>
    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Get Unread Count", Tags = new[] { "InAppNotification" })]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = int.Parse(_requestContext.UserId ?? "0");
        var count  = await _service.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(ApiResponse<int>.Ok(count, HttpContext.TraceIdentifier));
    }

    /// <summary>Mark specific notifications as read.</summary>
    [HttpPut("mark-read")]
    [SwaggerOperation(Summary = "Mark Notifications as Read", Tags = new[] { "InAppNotification" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(_requestContext.UserId ?? "0");
        await _service.MarkReadAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Mark all notifications as read for the current user.</summary>
    [HttpPut("mark-all-read")]
    [SwaggerOperation(Summary = "Mark All Notifications as Read", Tags = new[] { "InAppNotification" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = int.Parse(_requestContext.UserId ?? "0");
        await _service.MarkAllReadAsync(userId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
