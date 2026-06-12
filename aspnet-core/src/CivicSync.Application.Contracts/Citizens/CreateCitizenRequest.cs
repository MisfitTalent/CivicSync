using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Citizens;

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

    [MaxLength(60)]
    public string DateOfBirth { get; set; } = string.Empty;

    [MaxLength(30)]
    public string PassportNumber { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string BiometricReference { get; set; } = string.Empty;

    [MaxLength(200)]
    public string RelationshipStatus { get; set; } = string.Empty;

    [MaxLength(30)]
    public string TaxNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string EmploymentHistory { get; set; } = string.Empty;

    [MaxLength(500)]
    public string IncomeAndInvestmentProfile { get; set; } = string.Empty;

    [MaxLength(500)]
    public string BankingAndAssets { get; set; } = string.Empty;

    [MaxLength(300)]
    public string ResidentialAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string RatesAccount { get; set; } = string.Empty;

    [MaxLength(100)]
    public string MunicipalServiceStatus { get; set; } = string.Empty;
}
