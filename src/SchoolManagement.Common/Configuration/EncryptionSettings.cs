namespace SchoolManagement.Common.Configuration;

public sealed class EncryptionSettings
{
    /// <summary>Set false to disable payload encryption/decryption globally for all APIs.</summary>
    public bool Enabled { get; init; } = true;
    /// <summary>Base64-encoded 32-byte AES key (fallback / symmetric mode)</summary>
    public string AesKey { get; init; } = string.Empty;
    /// <summary>PEM-encoded RSA private key used for hybrid E2E encryption</summary>
    public string RsaPrivateKeyPem { get; init; } = string.Empty;
    /// <summary>Comma-separated path prefixes that bypass encryption middleware</summary>
    public string BypassPaths { get; init; } = "/swagger,/api/v1/auth,/api/v1/encryption";
}
