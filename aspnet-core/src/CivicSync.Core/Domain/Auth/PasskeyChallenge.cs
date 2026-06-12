using CivicSync.Core.Domain.Common;

namespace CivicSync.Core.Domain.Auth;

public sealed class PasskeyChallenge : EntityBase
{
    private PasskeyChallenge()
    {
    }

    public PasskeyChallenge(string emailAddress, string challenge, string purpose, DateTime expiresAtUtc)
    {
        EmailAddress = emailAddress;
        Challenge = challenge;
        Purpose = purpose;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string EmailAddress { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public bool IsAvailable(DateTime nowUtc)
    {
        return UsedAtUtc is null && ExpiresAtUtc > nowUtc;
    }

    public void MarkUsed()
    {
        UsedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
