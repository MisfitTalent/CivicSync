using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Auth;

public sealed class BeginPasskeyLoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;
}
