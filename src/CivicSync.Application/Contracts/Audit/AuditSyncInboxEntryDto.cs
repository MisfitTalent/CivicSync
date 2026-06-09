using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Audit;

public sealed class AuditSyncInboxEntryDto
{
    public Guid Id { get; set; }
    public Guid DepartmentNodeId { get; set; }
    public Guid LedgerEntryId { get; set; }
    public Guid ReceivedFromNodeId { get; set; }
    public string CitizenNationalIdNumber { get; set; } = string.Empty;
    public string FieldChangesJson { get; set; } = string.Empty;
    public SyncStatus Status { get; set; }
    public DateTime? AppliedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
