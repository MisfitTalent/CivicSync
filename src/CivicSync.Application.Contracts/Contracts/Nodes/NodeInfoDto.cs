using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Nodes;

public sealed record NodeInfoDto(
    DepartmentCode DepartmentCode,
    string ApiBaseUrl,
    IReadOnlyCollection<PeerNodeDto> Peers);
