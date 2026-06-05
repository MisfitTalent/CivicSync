using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Nodes;

public sealed record DepartmentNodeDto(
    Guid Id,
    DepartmentCode DepartmentCode,
    string ApiBaseUrl,
    NodeStatus Status,
    DateTime? LastSeenAtUtc,
    IReadOnlyCollection<PeerNodeDto> KnownPeers);
