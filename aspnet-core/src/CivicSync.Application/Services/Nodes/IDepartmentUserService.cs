using CivicSync.Application.Contracts.Nodes;

namespace CivicSync.Application.Services.Nodes;

public interface IDepartmentUserService
{
    Task<IReadOnlyCollection<DepartmentUserDto>> GetCurrentNodeUsersAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentUserDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DepartmentUserDto> CreateForCurrentNodeAsync(
        CreateDepartmentUserRequest request,
        CancellationToken cancellationToken = default);
}
