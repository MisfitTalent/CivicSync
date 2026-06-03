using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Node.Api.Infrastructure.Persistence.Seed;

public sealed class NodeDataSeeder
{
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly IRepository<DepartmentUser, Guid> _departmentUserRepository;
    private readonly NodeOptions _nodeOptions;

    public NodeDataSeeder(
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IRepository<DepartmentUser, Guid> departmentUserRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _departmentNodeRepository = departmentNodeRepository;
        _departmentUserRepository = departmentUserRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var departmentNodes = await _departmentNodeRepository.WithDetailsAsync(item => item.KnownPeers);
        var node = await departmentNodes.SingleOrDefaultAsync(
            item => item.DepartmentCode == _nodeOptions.DepartmentCode,
            cancellationToken);

        if (node is null)
        {
            node = new DepartmentNode(_nodeOptions.DepartmentCode, _nodeOptions.ApiBaseUrl);
            foreach (var peer in _nodeOptions.Peers)
            {
                node.RegisterPeer(peer.DepartmentCode, peer.ApiBaseUrl);
            }

            await _departmentNodeRepository.InsertAsync(node, autoSave: true, cancellationToken);
            await SeedDepartmentUsersAsync(node.Id, cancellationToken);
            return;
        }

        node.ApiBaseUrl = _nodeOptions.ApiBaseUrl;
        node.MarkOnline();

        foreach (var peer in _nodeOptions.Peers)
        {
            node.RegisterPeer(peer.DepartmentCode, peer.ApiBaseUrl);
        }

        await _departmentNodeRepository.UpdateAsync(node, autoSave: true, cancellationToken);
        await SeedDepartmentUsersAsync(node.Id, cancellationToken);
    }

    private async Task SeedDepartmentUsersAsync(Guid departmentNodeId, CancellationToken cancellationToken)
    {
        var users = await _departmentUserRepository.GetQueryableAsync();
        var existingEmails = await users
            .Where(item => item.DepartmentNodeId == departmentNodeId)
            .Select(item => item.EmailAddress)
            .ToListAsync(cancellationToken);

        foreach (var user in GetDemoUsers(departmentNodeId))
        {
            if (existingEmails.Contains(user.EmailAddress))
            {
                continue;
            }

            await _departmentUserRepository.InsertAsync(user, autoSave: true, cancellationToken);
        }
    }

    private IEnumerable<DepartmentUser> GetDemoUsers(Guid departmentNodeId)
    {
        return _nodeOptions.DepartmentCode switch
        {
            DepartmentCode.HomeAffairs =>
            [
                new DepartmentUser(departmentNodeId, "Naledi Mokoena", "Senior Identity Verifier", "naledi.mokoena@homeaffairs.gov.za"),
                new DepartmentUser(departmentNodeId, "Sipho Nkosi", "Home Affairs Supervisor", "sipho.nkosi@homeaffairs.gov.za")
            ],
            DepartmentCode.Sars =>
            [
                new DepartmentUser(departmentNodeId, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za"),
                new DepartmentUser(departmentNodeId, "Lerato Khumalo", "SARS Approval Manager", "lerato.khumalo@sars.gov.za")
            ],
            DepartmentCode.Municipality =>
            [
                new DepartmentUser(departmentNodeId, "Ayesha Patel", "Municipal Records Officer", "ayesha.patel@municipality.gov.za"),
                new DepartmentUser(departmentNodeId, "Johan van Wyk", "Municipal Services Supervisor", "johan.vanwyk@municipality.gov.za")
            ],
            _ =>
            [
                new DepartmentUser(departmentNodeId, "Demo Approver", "Department Approver", "approver@civicsync.local"),
                new DepartmentUser(departmentNodeId, "Demo Supervisor", "Department Supervisor", "supervisor@civicsync.local")
            ]
        };
    }
}

