using System.ComponentModel.DataAnnotations;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Sync;

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

    [Required]
    [MinLength(1)]
    public List<SyncedFieldChangeDto> FieldChanges { get; set; } = [];
}
