using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;
using Swashbuckle.AspNetCore.Annotations;

namespace SchoolManagement.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class EncryptionController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionController(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Returns the server's RSA-2048 public key in PEM format.
    /// Clients use this key to RSA-OAEP-encrypt their AES-256-GCM session key
    /// which is then sent in the X-Encrypted-Key header with every request.
    /// </summary>
    [HttpGet("public-key")]
    [SwaggerOperation(Summary = "Get Server RSA Public Key", Tags = new[] { "Encryption" })]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public IActionResult GetPublicKey()
    {
        var pem = _encryptionService.GetPublicKeyPem();
        return Ok(ApiResponse<string>.Ok(pem, HttpContext.TraceIdentifier));
    }
}
