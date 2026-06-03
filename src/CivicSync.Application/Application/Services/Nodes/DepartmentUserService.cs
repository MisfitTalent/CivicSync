using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Contracts.Nodes;
using CivicSync.Node.Api.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Node.Api.Application.Services.Nodes;

public sealed class DepartmentUserService : IDepartmentUserService
{
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly IRepository<DepartmentUser, Guid> _departmentUserRepository;
    private readonly NodeOptions _nodeOptions;

    public DepartmentUserService(
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IRepository<DepartmentUser, Guid> departmentUserRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _departmentNodeRepository = departmentNodeRepository;
        _departmentUserRepository = departmentUserRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task<IReadOnlyCollection<DepartmentUserDto>> GetCurrentNodeUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var currentNodeId = await GetCurrentNodeIdAsync(cancellationToken);
        var users = await _departmentUserRepository.GetQueryableAsync();

        return await users
            .Where(item => item.DepartmentNodeId == currentNodeId)
            .OrderBy(item => item.FullName)
            .Select(item => new DepartmentUserDto(
                item.Id,
                item.DepartmentNodeId,
                item.FullName,
                item.Role,
                item.EmailAddress,
                item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentUserDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentNodeId = await GetCurrentNodeIdAsync(cancellationToken);
        var users = await _departmentUserRepository.GetQueryableAsync();
        var user = await users
            .Where(item => item.Id == id && item.DepartmentNodeId == currentNodeId)
            .Select(item => new DepartmentUserDto(
                item.Id,
                item.DepartmentNodeId,
                item.FullName,
                item.Role,
                item.EmailAddress,
                item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

        return user ?? throw new InvalidOperationException("Department user was not found.");
    }

    private async Task<Guid> GetCurrentNodeIdAsync(CancellationToken cancellationToken)
    {
        var nodes = await _departmentNodeRepository.GetQueryableAsync();
        var node = await nodes.SingleOrDefaultAsync(
            item => item.DepartmentCode == _nodeOptions.DepartmentCode,
            cancellationToken);

        return node?.Id ?? throw new InvalidOperationException("Current department node was not found.");
    }
}
