using CivicSync.Application.Contracts.Nodes;

namespace CivicSync.Application.Services.Nodes;

public interface IDepartmentNodeService
{
    Task<IReadOnlyCollection<DepartmentNodeDto>> GetRegisteredNodesAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentNodeDto> RegisterAsync(
        RegisterDepartmentNodeRequest request,
        CancellationToken cancellationToken = default);
}
