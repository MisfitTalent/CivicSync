using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Configuration;

public sealed class NodeOptions
{
    public const string SectionName = "Node";

    public DepartmentCode DepartmentCode { get; set; } = DepartmentCode.HomeAffairs;
    public string ApiBaseUrl { get; set; } = "https://localhost:7001";
    public string SharedSecret { get; set; } = "development-node-sync-secret";
    public int MaxSyncPublishAttempts { get; set; } = 3;
    public List<PeerNodeOptions> Peers { get; set; } = [];
}
