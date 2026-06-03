using CivicSync.Node.Api.Domain.Common;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Domain.Nodes;

public sealed class DepartmentNode : EntityBase
{
    private readonly List<KnownPeerNode> _knownPeers = [];

    private DepartmentNode()
    {
    }

    public DepartmentNode(DepartmentCode departmentCode, string apiBaseUrl)
    {
        DepartmentCode = departmentCode;
        ApiBaseUrl = apiBaseUrl;
        Status = NodeStatus.Online;
        LastSeenAtUtc = DateTime.UtcNow;
    }

    public DepartmentCode DepartmentCode { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public NodeStatus Status { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public IReadOnlyCollection<KnownPeerNode> KnownPeers => _knownPeers.AsReadOnly();

    public void MarkOnline()
    {
        Status = NodeStatus.Online;
        LastSeenAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkOffline()
    {
        Status = NodeStatus.Offline;
        MarkUpdated();
    }

    public void RegisterPeer(DepartmentCode peerDepartmentCode, string peerBaseUrl)
    {
        if (_knownPeers.Any(peer => peer.PeerDepartmentCode == peerDepartmentCode))
        {
            return;
        }

        _knownPeers.Add(new KnownPeerNode(Id, peerDepartmentCode, peerBaseUrl));
        MarkUpdated();
    }
}
