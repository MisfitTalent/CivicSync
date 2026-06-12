namespace CivicSync.Application.Contracts.Auth;

public sealed class PasskeyChallengeResponse
{
    public string Challenge { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string RpName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TimeoutMs { get; set; }
    public IReadOnlyCollection<string> AllowedCredentialIds { get; set; } = [];
}
