using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Auth;

public sealed class BeginPasskeyRegistrationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
}
