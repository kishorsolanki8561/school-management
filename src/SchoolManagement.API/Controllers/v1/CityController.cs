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
public sealed class CityController : ControllerBase
{
    private readonly ICityService _cityService;

    public CityController(ICityService cityService)
    {
        _cityService = cityService;
    }

    /// <summary>Create a new city.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create City", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<CityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCityRequest request, CancellationToken cancellationToken)
    {
        var result = await _cityService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<CityResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing city.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update City", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<CityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCityRequest request, CancellationToken cancellationToken)
    {
        var result = await _cityService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CityResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a city.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete City", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _cityService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a city by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get City by Id", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<CityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _cityService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"City {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<CityResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of cities.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Cities", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CityResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _cityService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<CityResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get cities filtered by state.</summary>
    [HttpGet("by-state/{stateId:int}")]
    [SwaggerOperation(Summary = "Get Cities by State", Tags = new[] { "Master - City" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CityResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByState(int stateId, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _cityService.GetByStateAsync(stateId, pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<CityResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
