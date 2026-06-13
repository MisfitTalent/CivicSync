using CivicSync.Core.Configuration;
using CivicSync.Application.Contracts.Nodes;
using CivicSync.Core.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Nodes;

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

    public async Task<DepartmentUserDto> CreateForCurrentNodeAsync(
        CreateDepartmentUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentNodeId = await GetCurrentNodeIdAsync(cancellationToken);
        var fullName = request.FullName.Trim();
        var role = request.Role.Trim();
        var emailAddress = request.EmailAddress.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Department user full name is required.");
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new InvalidOperationException("Department user role is required.");
        }

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new InvalidOperationException("Department user email address is required.");
        }

        await EnsureEmailAddressIsAvailableAsync(currentNodeId, emailAddress, cancellationToken);

        var departmentUser = new DepartmentUser(
            currentNodeId,
            fullName,
            role,
            emailAddress);

        var createdUser = await _departmentUserRepository.InsertAsync(
            departmentUser,
            autoSave: true,
            cancellationToken);

        return MapToDto(createdUser);
    }

    private async Task<Guid> GetCurrentNodeIdAsync(CancellationToken cancellationToken)
    {
        var nodes = await _departmentNodeRepository.GetQueryableAsync();
        var node = await nodes.SingleOrDefaultAsync(
            item => item.DepartmentCode == _nodeOptions.DepartmentCode,
            cancellationToken);

        return node?.Id ?? throw new InvalidOperationException("Current department node was not found.");
    }

    private async Task EnsureEmailAddressIsAvailableAsync(
        Guid departmentNodeId,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var users = await _departmentUserRepository.GetQueryableAsync();
        var alreadyExists = await users.AnyAsync(
            item => item.DepartmentNodeId == departmentNodeId &&
                    item.EmailAddress == emailAddress,
            cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException("A department user with this email address already exists on this node.");
        }
    }

    private static DepartmentUserDto MapToDto(DepartmentUser user)
    {
        return new DepartmentUserDto(
            user.Id,
            user.DepartmentNodeId,
            user.FullName,
            user.Role,
            user.EmailAddress,
            user.IsActive);
    }
}
