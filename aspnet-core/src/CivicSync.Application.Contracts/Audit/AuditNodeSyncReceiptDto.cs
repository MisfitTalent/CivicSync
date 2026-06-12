using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Audit;

public sealed class AuditNodeSyncReceiptDto
{
    public Guid Id { get; set; }
    public Guid SyncOutboxEventId { get; set; }
    public Guid TargetNodeId { get; set; }
    public SyncResult Result { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
