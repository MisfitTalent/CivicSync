using CivicSync.Core.Domain.Common;

namespace CivicSync.Core.Domain.Auth;

public sealed class PasskeyCredential : EntityBase
{
    private PasskeyCredential()
    {
    }

    public PasskeyCredential(
        string emailAddress,
        string credentialId,
        string publicKey,
        int publicKeyAlgorithm,
        string displayName)
    {
        EmailAddress = emailAddress;
        CredentialId = credentialId;
        PublicKey = publicKey;
        PublicKeyAlgorithm = publicKeyAlgorithm;
        DisplayName = displayName;
    }

    public string EmailAddress { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public int PublicKeyAlgorithm { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public uint SignCount { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }

    public void RecordSuccessfulAuthentication(uint signCount)
    {
        SignCount = Math.Max(SignCount, signCount);
        LastUsedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
