using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Audit;

public sealed class AuditSyncOutboxEventDto
{
    public Guid Id { get; set; }
    public Guid DepartmentNodeId { get; set; }
    public Guid LedgerEntryId { get; set; }
    public SyncStatus Status { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
