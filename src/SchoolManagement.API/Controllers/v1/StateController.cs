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
public sealed class StateController : ControllerBase
{
    private readonly IStateService _stateService;

    public StateController(IStateService stateService)
    {
        _stateService = stateService;
    }

    /// <summary>Create a new state.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create State", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<StateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStateRequest request, CancellationToken cancellationToken)
    {
        var result = await _stateService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<StateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing state.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update State", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<StateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStateRequest request, CancellationToken cancellationToken)
    {
        var result = await _stateService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<StateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a state.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete State", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _stateService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a state by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get State by Id", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<StateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _stateService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"State {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<StateResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of states.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All States", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _stateService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<StateResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get states filtered by country.</summary>
    [HttpGet("by-country/{countryId:int}")]
    [SwaggerOperation(Summary = "Get States by Country", Tags = new[] { "Master - State" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCountry(int countryId, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _stateService.GetByCountryAsync(countryId, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<StateResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
