namespace CivicSync.Application.Contracts.Citizens;

public class BiometricVerificationResult
{
    public Guid CitizenId { get; set; }
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
}
