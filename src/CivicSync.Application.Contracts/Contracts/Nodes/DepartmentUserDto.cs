namespace CivicSync.Node.Api.Contracts.Nodes;

public sealed record DepartmentUserDto(
    Guid Id,
    Guid DepartmentNodeId,
    string FullName,
    string Role,
    string EmailAddress,
    bool IsActive);
