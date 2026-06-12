using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Auth;

public sealed class CompletePasskeyLoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    public string CredentialId { get; set; } = string.Empty;

    [Required]
    public string ClientDataJson { get; set; } = string.Empty;

    [Required]
    public string AuthenticatorData { get; set; } = string.Empty;

    [Required]
    public string Signature { get; set; } = string.Empty;
}
