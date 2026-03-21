using System.Security.Cryptography;
using System.Text;
using SchoolManagement.Common.Configuration;

namespace SchoolManagement.Common.Services;

public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[] _aesKey;
    private readonly RSA _rsa;

    public EncryptionService()
    {
        var settings = InitializeConfiguration.EncryptionSettings;
        _aesKey = Convert.FromBase64String(settings.AesKey);
        _rsa = RSA.Create(2048);

        if (!string.IsNullOrWhiteSpace(settings.RsaPrivateKeyPem))
            _rsa.ImportFromPem(settings.RsaPrivateKeyPem);
    }

    private const int AesNonceSize = 12; // AES-GCM nonce is always 12 bytes
    private const int AesTagSize = 16;   // AES-GCM tag is always 16 bytes

    public string Encrypt(string plaintext)
    {
        var nonce = new byte[AesNonceSize];
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesTagSize];

        using var aesGcm = new AesGcm(_aesKey);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Combine: nonce(12) + tag(16) + ciphertext
        var combined = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(combined);
    }

    public string Decrypt(string encryptedBase64)
    {
        var combined = Convert.FromBase64String(encryptedBase64);

        var nonce = combined[..AesNonceSize];
        var tag = combined[AesNonceSize..(AesNonceSize + AesTagSize)];
        var ciphertext = combined[(AesNonceSize + AesTagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(_aesKey);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public string GetPublicKeyPem()
    {
        var keyDer = _rsa.ExportSubjectPublicKeyInfo();
        return "-----BEGIN PUBLIC KEY-----\n"
            + Convert.ToBase64String(keyDer, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PUBLIC KEY-----";
    }

    public byte[] DecryptSessionKey(string encryptedSessionKeyBase64)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedSessionKeyBase64);
        return _rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] EncryptWithSessionKey(byte[] sessionKey, byte[] nonce, string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[AesTagSize];

        using var aesGcm = new AesGcm(sessionKey);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[tag.Length + ciphertext.Length];
        Buffer.BlockCopy(tag, 0, result, 0, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, tag.Length, ciphertext.Length);
        return result;
    }

    public string DecryptWithSessionKey(byte[] sessionKey, byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(sessionKey);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
