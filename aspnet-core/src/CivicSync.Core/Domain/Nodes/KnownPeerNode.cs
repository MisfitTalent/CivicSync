using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.Nodes;

public sealed class KnownPeerNode : EntityBase
{
    private KnownPeerNode()
    {
    }

    public KnownPeerNode(Guid departmentNodeId, DepartmentCode peerDepartmentCode, string peerBaseUrl)
    {
        DepartmentNodeId = departmentNodeId;
        PeerDepartmentCode = peerDepartmentCode;
        PeerBaseUrl = peerBaseUrl;
    }

    public Guid DepartmentNodeId { get; set; }
    public DepartmentCode PeerDepartmentCode { get; set; }
    public string PeerBaseUrl { get; set; } = string.Empty;
    public long LastSyncedSequence { get; set; }

    public void UpdateCheckpoint(long ledgerSequence)
    {
        if (ledgerSequence <= LastSyncedSequence)
        {
            return;
        }

        LastSyncedSequence = ledgerSequence;
        MarkUpdated();
    }
}
