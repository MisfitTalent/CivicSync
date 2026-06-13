using System.ComponentModel.DataAnnotations;

namespace CivicSync.Application.Contracts.Nodes;

public sealed record CreateDepartmentUserRequest(
    [property: Required, MaxLength(160)] string FullName,
    [property: Required, MaxLength(120)] string Role,
    [property: Required, EmailAddress, MaxLength(160)] string EmailAddress);
