using System.ComponentModel.DataAnnotations;

namespace CivicSync.Node.Api.Contracts.Citizens;

public sealed class CreateCitizenRequest
{
    [Required]
    [MaxLength(30)]
    public string NationalIdNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;
}
