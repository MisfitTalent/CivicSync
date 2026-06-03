using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Nodes;

public sealed record PeerNodeDto(DepartmentCode DepartmentCode, string ApiBaseUrl);
