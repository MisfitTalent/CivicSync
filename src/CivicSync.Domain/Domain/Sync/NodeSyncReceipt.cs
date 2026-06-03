using CivicSync.Node.Api.Domain.Common;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Domain.Sync;

public sealed class NodeSyncReceipt : EntityBase
{
    private NodeSyncReceipt()
    {
    }

    public NodeSyncReceipt(Guid syncOutboxEventId, Guid targetNodeId, SyncResult result)
    {
        SyncOutboxEventId = syncOutboxEventId;
        TargetNodeId = targetNodeId;
        Result = result;
        ReceivedAtUtc = DateTime.UtcNow;
    }

    public Guid SyncOutboxEventId { get; set; }
    public Guid TargetNodeId { get; set; }
    public SyncResult Result { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}
