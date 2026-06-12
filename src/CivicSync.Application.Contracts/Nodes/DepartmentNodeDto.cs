using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Nodes;

public sealed record DepartmentNodeDto(
    Guid Id,
    DepartmentCode DepartmentCode,
    string ApiBaseUrl,
    NodeStatus Status,
    DateTime? LastSeenAtUtc,
    IReadOnlyCollection<PeerNodeDto> KnownPeers);
