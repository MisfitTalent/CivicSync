using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Nodes;

public sealed record PeerNodeDto(DepartmentCode DepartmentCode, string ApiBaseUrl);
