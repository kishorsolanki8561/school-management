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
public sealed class CountryController : ControllerBase
{
    private readonly ICountryService _countryService;

    public CountryController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    /// <summary>Create a new country.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Create Country", Tags = new[] { "Master - Country" })]
    [ProducesResponseType(typeof(ApiResponse<CountryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request, CancellationToken cancellationToken)
    {
        var result = await _countryService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<CountryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Update an existing country.</summary>
    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update Country", Tags = new[] { "Master - Country" })]
    [ProducesResponseType(typeof(ApiResponse<CountryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCountryRequest request, CancellationToken cancellationToken)
    {
        var result = await _countryService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CountryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Soft-delete a country.</summary>
    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete Country", Tags = new[] { "Master - Country" })]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _countryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Get a country by id.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get Country by Id", Tags = new[] { "Master - Country" })]
    [ProducesResponseType(typeof(ApiResponse<CountryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _countryService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail(ErrorDetail.NotFound($"Country {id} not found."), HttpContext.TraceIdentifier));

        return Ok(ApiResponse<CountryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Get paginated list of countries.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get All Countries", Tags = new[] { "Master - Country" })]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CountryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var result = await _countryService.GetAllAsync(pagination, cancellationToken);
        return Ok(ApiResponse<PagedResult<CountryResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
