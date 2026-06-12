using System.Security.Cryptography;
using System.Text;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Security;

/// <summary>
/// Encrypts citizen master-record values before they are persisted by EF Core.
/// </summary>
public static class MasterRecordEncryption
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const string Prefix = "enc:v1:";
    private const string KeyEnvironmentVariable = "CIVICSYNC_MASTER_RECORD_KEY";
    private const string DevelopmentKeyMaterial = "civicsync-local-development-master-record-key";

    public static string Encrypt(string? value, string purpose)
    {
        if (string.IsNullOrEmpty(value) || IsEncrypted(value))
        {
            return value ?? string.Empty;
        }

        var key = GetEncryptionKey();
        var plaintext = Encoding.UTF8.GetBytes(value);
        var nonce = CreateDeterministicNonce(key, purpose, value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes(purpose));

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);

        return $"{Prefix}{Convert.ToBase64String(payload)}";
    }

    public static string Decrypt(string? value, string purpose)
    {
        if (string.IsNullOrEmpty(value) || !IsEncrypted(value))
        {
            return value ?? string.Empty;
        }

        var payload = Convert.FromBase64String(value[Prefix.Length..]);
        if (payload.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("Encrypted master-record payload is invalid.");
        }

        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var ciphertext = payload[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetEncryptionKey(), TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, Encoding.UTF8.GetBytes(purpose));

        return Encoding.UTF8.GetString(plaintext);
    }

    public static bool IsEncrypted(string value)
    {
        return value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    private static byte[] GetEncryptionKey()
    {
        var keyMaterial = Environment.GetEnvironmentVariable(KeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            keyMaterial = DevelopmentKeyMaterial;
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial.Trim()));
    }

    private static byte[] CreateDeterministicNonce(byte[] key, string purpose, string value)
    {
        using var hmac = new HMACSHA256(key);
        var nonceSource = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{purpose}|{value}"));
        return nonceSource[..NonceSize];
    }
}
