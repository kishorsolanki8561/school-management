namespace SchoolManagement.Common.Services;

public interface IEncryptionService
{
    /// <summary>Encrypt plaintext using AES-256-GCM. Returns Base64(nonce + tag + ciphertext).</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypt a value previously encrypted with <see cref="Encrypt"/>.</summary>
    string Decrypt(string encryptedBase64);

    /// <summary>Returns the server's RSA public key in PEM format for client key-exchange.</summary>
    string GetPublicKeyPem();

    /// <summary>Decrypt an AES session key that was RSA-encrypted by the client.</summary>
    byte[] DecryptSessionKey(string encryptedSessionKeyBase64);

    /// <summary>Encrypt plaintext using a client-provided AES session key and nonce.</summary>
    byte[] EncryptWithSessionKey(byte[] sessionKey, byte[] nonce, string plaintext);

    /// <summary>Decrypt ciphertext using a client-provided AES session key and nonce.</summary>
    string DecryptWithSessionKey(byte[] sessionKey, byte[] nonce, byte[] ciphertext, byte[] tag);
}
