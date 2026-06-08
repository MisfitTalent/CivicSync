using CivicSync.Node.Api.Contracts.Nodes;

namespace CivicSync.Node.Api.Application.Services.Nodes;

public interface IDepartmentNodeService
{
    Task<IReadOnlyCollection<DepartmentNodeDto>> GetRegisteredNodesAsync(
        CancellationToken cancellationToken = default);

    Task<DepartmentNodeDto> RegisterAsync(
        RegisterDepartmentNodeRequest request,
        CancellationToken cancellationToken = default);
}
