using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class FileUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;

    public FileUploadController(IFileUploadService fileUploadService)
        => _fileUploadService = fileUploadService;

    /// <summary>Upload one or more files for a specific org and screen.</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Upload Files", Tags = new[] { "File Upload" })]
    [ProducesResponseType(typeof(ApiResponse<IList<FileUploadResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromQuery] int? pageId,
        [FromQuery] int? orgId,
        [FromForm] IList<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var result = await _fileUploadService.UploadAsync(files, pageId, orgId, cancellationToken);
        return Ok(ApiResponse<IList<FileUploadResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
