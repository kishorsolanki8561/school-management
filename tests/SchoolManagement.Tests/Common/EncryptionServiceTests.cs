using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Services;
using Xunit;

namespace SchoolManagement.Tests.Common;

public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        // Generate valid AES-256 key (32 bytes) and RSA key for tests
        var aesKey = new byte[32];
        RandomNumberGenerator.Fill(aesKey);

        using var rsa = RSA.Create(2048);
        var keyDer = rsa.ExportPkcs8PrivateKey();
        var privateKeyPem = "-----BEGIN PRIVATE KEY-----\n"
            + Convert.ToBase64String(keyDer, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END PRIVATE KEY-----";

        // Bootstrap InitializeConfiguration with test settings
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["EncryptionSettings:AesKey"]            = Convert.ToBase64String(aesKey),
                ["EncryptionSettings:RsaPrivateKeyPem"]  = privateKeyPem,
                ["JwtSettings:SecretKey"]                = "test-secret",
                ["JwtSettings:Issuer"]                   = "test",
                ["JwtSettings:Audience"]                 = "test",
                ["ConnectionStrings:DefaultConnection"]  = "Server=test;Database=test;"
            })
            .Build();

        InitializeConfiguration.Initialize(config);

        _sut = new EncryptionService();
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ShouldReturnOriginalText()
    {
        const string plaintext = "Hello, World! 123 !@#";

        var encrypted = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentCiphertextEachCall()
    {
        const string plaintext = "same input";

        var enc1 = _sut.Encrypt(plaintext);
        var enc2 = _sut.Encrypt(plaintext);

        enc1.Should().NotBe(enc2); // Different nonces => different ciphertext
    }

    [Fact]
    public void Decrypt_ShouldThrow_WhenInputTampered()
    {
        var encrypted = _sut.Encrypt("test");
        var bytes = Convert.FromBase64String(encrypted);
        bytes[^1] ^= 0xFF; // Flip last byte (tamper with ciphertext)
        var tampered = Convert.ToBase64String(bytes);

        _sut.Invoking(s => s.Decrypt(tampered))
            .Should().Throw<Exception>();
    }

    [Fact]
    public void GetPublicKeyPem_ShouldReturnValidPemString()
    {
        var pem = _sut.GetPublicKeyPem();

        pem.Should().StartWith("-----BEGIN PUBLIC KEY-----");
    }

    [Fact]
    public void DecryptSessionKey_EncryptWithSessionKey_ShouldRoundtrip()
    {
        // Simulate client encrypting a session key with server public key
        var publicKeyPem = _sut.GetPublicKeyPem();
        using var clientRsa = RSA.Create();
        clientRsa.ImportFromPem(publicKeyPem);

        var sessionKey = new byte[32];
        RandomNumberGenerator.Fill(sessionKey);
        var encryptedKey = clientRsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
        var encryptedKeyBase64 = Convert.ToBase64String(encryptedKey);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        const string message = "secret payload";

        // Server decrypts session key and decrypts message
        var decryptedKey = _sut.DecryptSessionKey(encryptedKeyBase64);
        var encryptedPayload = _sut.EncryptWithSessionKey(decryptedKey, nonce, message);

        var tag = encryptedPayload[..16];
        var ciphertext = encryptedPayload[16..];
        var decrypted = _sut.DecryptWithSessionKey(decryptedKey, nonce, ciphertext, tag);

        decrypted.Should().Be(message);
    }
}
