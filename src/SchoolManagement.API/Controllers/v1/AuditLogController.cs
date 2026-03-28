using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-log")]
[Produces("application/json")]
[Authorize]
public sealed class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
        => _auditLogService = auditLogService;

    /// <summary>Get paginated audit history for a specific record.</summary>
    [HttpGet("entity/{entityName}/{entityId}")]
    [SwaggerOperation(Summary = "Get Audit Logs by Entity", Tags = new[] { "Audit Log" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(
        string entityName,
        string entityId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByEntityAsync(entityName, entityId, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLog>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated audit logs created by a specific user.</summary>
    [HttpGet("user/{userId}")]
    [SwaggerOperation(Summary = "Get Audit Logs by User", Tags = new[] { "Audit Log" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(
        string userId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByUserAsync(userId, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLog>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated audit logs triggered from a specific screen.</summary>
    [HttpGet("screen/{screenName}")]
    [SwaggerOperation(Summary = "Get Audit Logs by Screen", Tags = new[] { "Audit Log" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByScreen(
        string screenName,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByScreenAsync(screenName, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLog>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated audit logs for a specific database table.</summary>
    [HttpGet("table/{tableName}")]
    [SwaggerOperation(Summary = "Get Audit Logs by Table", Tags = new[] { "Audit Log" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLog>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTable(
        string tableName,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByTableAsync(tableName, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLog>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Get paginated audit history for a specific entity with full batch hierarchy.
    /// Each page item is one DB-transaction batch; root nodes contain child records
    /// (e.g. PageMasterModules nested under PageMaster) linked via ParentAuditLogId.
    /// </summary>
    [HttpGet("entity/{entityName}/{entityId}/hierarchy")]
    [SwaggerOperation(Summary = "Get Audit Log Hierarchy by Entity", Tags = new[] { "Audit Log" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogBatchResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntityHierarchy(
        string entityName,
        string entityId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByEntityHierarchyAsync(
            entityName, entityId, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogBatchResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
