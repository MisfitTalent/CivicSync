using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Auth;

public sealed class CompletePasskeyRegistrationRequest
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
    public string PublicKey { get; set; } = string.Empty;

    public int PublicKeyAlgorithm { get; set; }
}
