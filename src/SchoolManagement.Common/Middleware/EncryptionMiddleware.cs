using System.Text;
using Microsoft.AspNetCore.Http;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Services;

namespace SchoolManagement.Common.Middleware;

/// <summary>
/// Hybrid E2E encryption middleware.
/// Client flow:
///   1. GET /api/v1/encryption/public-key  → server RSA-2048 public key (PEM)
///   2. Client generates AES-256-GCM session key + 12-byte nonce
///   3. Client sends: X-Encrypted-Key (RSA-OAEP-SHA256 encrypted AES key, Base64)
///                    X-Encryption-Nonce (Base64 nonce)
///                    Body: AES-GCM encrypted JSON (tag appended to ciphertext)
///   4. Server decrypts body before pipeline; encrypts response body before sending.
/// Endpoints matching BypassPaths skip encryption.
/// Set EncryptionSettings:Enabled = false in appsettings to disable globally.
/// </summary>
public sealed class EncryptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _bypassPaths;
    private readonly bool _enabled;

    public EncryptionMiddleware(RequestDelegate next)
    {
        _next = next;
        var settings = InitializeConfiguration.EncryptionSettings;
        _enabled = settings.Enabled;
        _bypassPaths = settings.BypassPaths
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task InvokeAsync(HttpContext context, IEncryptionService encryptionService)
    {
        if (!_enabled || ShouldBypass(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // --- Decrypt request body ---
        var encryptedKeyHeader = context.Request.Headers[AppMessages.Encryption.EncryptedKeyHeader].FirstOrDefault();
        var nonceHeader = context.Request.Headers[AppMessages.Encryption.RequestNonceHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(encryptedKeyHeader) && !string.IsNullOrEmpty(nonceHeader))
        {
            var sessionKey = encryptionService.DecryptSessionKey(encryptedKeyHeader);
            var nonce = Convert.FromBase64String(nonceHeader);

            context.Request.EnableBuffering();
            var encryptedBody = await ReadBodyAsync(context.Request.Body);
            context.Request.Body.Position = 0;

            if (encryptedBody.Length > 0)
            {
                const int tagSize = 16;
                var tag = encryptedBody[..tagSize];
                var ciphertext = encryptedBody[tagSize..];
                var decryptedJson = encryptionService.DecryptWithSessionKey(sessionKey, nonce, ciphertext, tag);
                var decryptedBytes = Encoding.UTF8.GetBytes(decryptedJson);
                context.Request.Body = new MemoryStream(decryptedBytes);
                context.Request.ContentLength = decryptedBytes.Length;
            }

            // --- Capture and encrypt response body ---
            var originalBody = context.Response.Body;
            using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            await _next(context);

            buffer.Seek(0, SeekOrigin.Begin);
            var responseJson = await new StreamReader(buffer).ReadToEndAsync();

            if (!string.IsNullOrEmpty(responseJson))
            {
                var responseNonce = new byte[12];
                System.Security.Cryptography.RandomNumberGenerator.Fill(responseNonce);
                var encrypted = encryptionService.EncryptWithSessionKey(sessionKey, responseNonce, responseJson);
                context.Response.Headers[AppMessages.Encryption.ResponseNonceHeader] = Convert.ToBase64String(responseNonce);
                context.Response.ContentType = "application/octet-stream";
                context.Response.ContentLength = encrypted.Length;
                context.Response.Body = originalBody;
                await context.Response.Body.WriteAsync(encrypted);
            }
            else
            {
                context.Response.Body = originalBody;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private bool ShouldBypass(PathString path) =>
        _bypassPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static async Task<byte[]> ReadBodyAsync(Stream body)
    {
        using var ms = new MemoryStream();
        await body.CopyToAsync(ms);
        return ms.ToArray();
    }
}
