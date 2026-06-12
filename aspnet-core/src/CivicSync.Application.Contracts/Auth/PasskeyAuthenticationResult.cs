namespace CivicSync.Application.Contracts.Auth;

public sealed class PasskeyAuthenticationResult
{
    public bool IsAuthenticated { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
