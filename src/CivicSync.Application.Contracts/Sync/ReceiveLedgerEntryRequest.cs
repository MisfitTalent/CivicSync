using System.ComponentModel.DataAnnotations;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Sync;

public sealed class ReceiveLedgerEntryRequest
{
    [Required]
    public Guid LedgerEntryId { get; set; }

    [Required]
    public Guid OriginatingNodeId { get; set; }

    [Required]
    public Guid ChangeRequestId { get; set; }

    [Required]
    public long SequenceNumber { get; set; }

    [Required]
    public LedgerEventType EventType { get; set; }

    [Required]
    [MaxLength(256)]
    public string PayloadProofHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string PreviousProofHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string CurrentProofHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string CitizenNationalIdNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CitizenFirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CitizenLastName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string CitizenEmailAddress { get; set; } = string.Empty;

    [MaxLength(30)]
    public string CitizenPhoneNumber { get; set; } = string.Empty;

    [MaxLength(60)]
    public string CitizenDateOfBirth { get; set; } = string.Empty;

    [MaxLength(30)]
    public string CitizenPassportNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CitizenBiometricReference { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CitizenRelationshipStatus { get; set; } = string.Empty;

    [MaxLength(30)]
    public string CitizenTaxNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CitizenEmploymentHistory { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CitizenIncomeAndInvestmentProfile { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CitizenBankingAndAssets { get; set; } = string.Empty;

    [MaxLength(300)]
    public string CitizenResidentialAddress { get; set; } = string.Empty;

    [MaxLength(50)]
    public string CitizenRatesAccount { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CitizenMunicipalServiceStatus { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<SyncedFieldChangeDto> FieldChanges { get; set; } = [];
}
