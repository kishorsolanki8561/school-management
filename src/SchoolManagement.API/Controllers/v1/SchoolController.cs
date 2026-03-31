using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/school")]
[Produces("application/json")]
[Authorize]
public sealed class SchoolController : ControllerBase
{
    private readonly ISchoolService _schoolService;

    public SchoolController(ISchoolService schoolService)
    {
        _schoolService = schoolService;
    }

    /// <summary>Admin submits a school registration request.</summary>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register School", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<SchoolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterSchoolRequest request, CancellationToken cancellationToken)
    {
        var result = await _schoolService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<SchoolResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update school info (Admin of that school or OwnerAdmin).</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update School", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<SchoolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSchoolRequest request, CancellationToken cancellationToken)
    {
        var result = await _schoolService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchoolResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a school (OwnerAdmin only).</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete School", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _schoolService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a school by ID.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get School by Id", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<SchoolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _schoolService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"School {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<SchoolResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of schools (OwnerAdmin sees all).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Schools", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SchoolResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] bool? isApproved = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _schoolService.GetAllAsync(pagination, isApproved, cancellationToken);
        return Ok(ApiResponse<PagedResult<SchoolResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>OwnerAdmin approves a school registration.</summary>
    [HttpPut("{id:int}/approve")]
    [SwaggerOperation(Summary = "Approve School", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<SchoolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await _schoolService.ApproveAsync(id, cancellationToken);
        return Ok(ApiResponse<SchoolResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>OwnerAdmin rejects a school registration with a reason.</summary>
    [HttpPut("{id:int}/reject")]
    [SwaggerOperation(Summary = "Reject School", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<SchoolResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectSchoolRequest request, CancellationToken cancellationToken)
    {
        var result = await _schoolService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchoolResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get all pending approval requests (OwnerAdmin only).</summary>
    [HttpGet("pending-approvals")]
    [SwaggerOperation(Summary = "Get Pending Approvals", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ApprovalRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _schoolService.GetPendingApprovalsAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<ApprovalRequestResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get approval history for a specific school.</summary>
    [HttpGet("{id:int}/approval-history")]
    [SwaggerOperation(Summary = "Get School Approval History", Tags = new[] { "School" })]
    [ProducesResponseType(typeof(ApiResponse<IList<ApprovalRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalHistory(int id, CancellationToken cancellationToken)
    {
        var result = await _schoolService.GetApprovalHistoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IList<ApprovalRequestResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
