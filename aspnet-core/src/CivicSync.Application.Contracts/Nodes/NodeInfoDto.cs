using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Nodes;

public sealed record NodeInfoDto(
    DepartmentCode DepartmentCode,
    string ApiBaseUrl,
    IReadOnlyCollection<PeerNodeDto> Peers);
