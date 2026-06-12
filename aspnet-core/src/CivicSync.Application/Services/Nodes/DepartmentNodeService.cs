using CivicSync.Core.Configuration;
using CivicSync.Application.Contracts.Nodes;
using CivicSync.Core.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Nodes;

public sealed class DepartmentNodeService : IDepartmentNodeService
{
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly NodeOptions _nodeOptions;

    public DepartmentNodeService(
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _departmentNodeRepository = departmentNodeRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task<IReadOnlyCollection<DepartmentNodeDto>> GetRegisteredNodesAsync(
        CancellationToken cancellationToken = default)
    {
        var nodes = await _departmentNodeRepository.WithDetailsAsync(item => item.KnownPeers);

        var registeredNodes = await nodes
            .OrderBy(item => item.DepartmentCode)
            .ToListAsync(cancellationToken);

        return registeredNodes
            .Select(ToDto)
            .ToList();
    }

    public async Task<DepartmentNodeDto> RegisterAsync(
        RegisterDepartmentNodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = NormalizeApiBaseUrl(request.ApiBaseUrl);
        var nodes = await _departmentNodeRepository.WithDetailsAsync(item => item.KnownPeers);

        var existingNode = await nodes.SingleOrDefaultAsync(
            item => item.DepartmentCode == request.DepartmentCode,
            cancellationToken);

        if (existingNode is not null)
        {
            throw new InvalidOperationException("Department node is already registered.");
        }

        var departmentNode = new DepartmentNode(request.DepartmentCode, apiBaseUrl);
        await _departmentNodeRepository.InsertAsync(departmentNode, autoSave: false, cancellationToken);

        if (request.RegisterAsPeerOfCurrentNode && request.DepartmentCode != _nodeOptions.DepartmentCode)
        {
            var currentNode = await nodes.SingleOrDefaultAsync(
                item => item.DepartmentCode == _nodeOptions.DepartmentCode,
                cancellationToken);

            if (currentNode is null)
            {
                throw new InvalidOperationException("Current department node was not found.");
            }

            currentNode.RegisterPeer(request.DepartmentCode, apiBaseUrl);
            await _departmentNodeRepository.UpdateAsync(currentNode, autoSave: true, cancellationToken);
        }
        else
        {
            await _departmentNodeRepository.UpdateAsync(departmentNode, autoSave: true, cancellationToken);
        }

        return ToDto(departmentNode);
    }

    private static DepartmentNodeDto ToDto(DepartmentNode node)
    {
        var peers = node.KnownPeers
            .OrderBy(item => item.PeerDepartmentCode)
            .Select(item => new PeerNodeDto(item.PeerDepartmentCode, item.PeerBaseUrl))
            .ToList();

        return new DepartmentNodeDto(
            node.Id,
            node.DepartmentCode,
            node.ApiBaseUrl,
            node.Status,
            node.LastSeenAtUtc,
            peers);
    }

    private static string NormalizeApiBaseUrl(string apiBaseUrl)
    {
        var trimmedApiBaseUrl = apiBaseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmedApiBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Department API base URL must be an absolute HTTP or HTTPS URL.");
        }

        return trimmedApiBaseUrl;
    }
}

