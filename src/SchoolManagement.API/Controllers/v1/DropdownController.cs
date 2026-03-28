using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dropdown")]
[Produces("application/json")]
[Authorize]
public sealed class DropdownController : ControllerBase
{
    private readonly IDropdownService _dropdownService;

    public DropdownController(IDropdownService dropdownService)
    {
        _dropdownService = dropdownService;
    }

    /// <summary>
    /// Returns dropdown items for the specified key.
    /// Each item always has <c>name</c> and <c>value</c>; extra columns appear as additional camelCase properties.
    /// </summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Get Dropdown Data", Tags = new[] { "Dropdown" })]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Dictionary<string, object?>>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDropdown(
        [FromBody] DropdownRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dropdownService.GetDropdownAsync(request, cancellationToken);
        return Ok(ApiResponse<IEnumerable<Dictionary<string, object?>>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
