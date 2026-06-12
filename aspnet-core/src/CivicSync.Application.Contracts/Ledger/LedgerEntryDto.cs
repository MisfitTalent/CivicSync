using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Ledger;

public sealed class LedgerEntryDto
{
    public Guid Id { get; set; }
    public Guid OriginatingNodeId { get; set; }
    public Guid ChangeRequestId { get; set; }
    public long SequenceNumber { get; set; }
    public LedgerEventType EventType { get; set; }
    public string PayloadProofHash { get; set; } = string.Empty;
    public string PreviousProofHash { get; set; } = string.Empty;
    public string CurrentProofHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
