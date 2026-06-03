using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Application.Configuration;

public sealed class PeerNodeOptions
{
    public DepartmentCode DepartmentCode { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string SharedSecret { get; set; } = "development-node-sync-secret";
}
