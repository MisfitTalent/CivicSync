using CivicSync.Core.Configuration;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;

public sealed class NodeDataSeeder
{
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly IRepository<DepartmentUser, Guid> _departmentUserRepository;
    private readonly IRepository<Citizen, Guid> _citizenRepository;
    private readonly NodeOptions _nodeOptions;

    public NodeDataSeeder(
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IRepository<DepartmentUser, Guid> departmentUserRepository,
        IRepository<Citizen, Guid> citizenRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _departmentNodeRepository = departmentNodeRepository;
        _departmentUserRepository = departmentUserRepository;
        _citizenRepository = citizenRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seededNodes = new List<DepartmentNode>();
        foreach (var department in GetDemoDepartments())
        {
            var node = await EnsureDepartmentNodeAsync(
                department,
                GetDemoApiBaseUrl(department),
                cancellationToken);
            seededNodes.Add(node);
            await SeedDepartmentUsersAsync(node.Id, node.DepartmentCode, cancellationToken);
        }

        var localNode = seededNodes.Single(item => item.DepartmentCode == _nodeOptions.DepartmentCode);
        localNode.ApiBaseUrl = _nodeOptions.ApiBaseUrl;
        localNode.MarkOnline();
        RegisterConfiguredPeers(localNode);
        await _departmentNodeRepository.UpdateAsync(localNode, autoSave: true, cancellationToken);
        await SeedDemoCitizensAsync(localNode.Id, cancellationToken);
    }

    private async Task<DepartmentNode> EnsureDepartmentNodeAsync(
        DepartmentCode departmentCode,
        string apiBaseUrl,
        CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.WithDetailsAsync(item => item.KnownPeers);
        var node = await departmentNodes.SingleOrDefaultAsync(
            item => item.DepartmentCode == departmentCode,
            cancellationToken);

        if (node is not null)
        {
            node.ApiBaseUrl = apiBaseUrl;
            node.MarkOnline();
            await _departmentNodeRepository.UpdateAsync(node, autoSave: true, cancellationToken);
            return node;
        }

        node = new DepartmentNode(departmentCode, apiBaseUrl);
        await _departmentNodeRepository.InsertAsync(node, autoSave: true, cancellationToken);

        return node;
    }

    private void RegisterConfiguredPeers(DepartmentNode localNode)
    {
        foreach (var peer in _nodeOptions.Peers)
        {
            localNode.RegisterPeer(peer.DepartmentCode, peer.ApiBaseUrl);
        }
    }

    private async Task SeedDepartmentUsersAsync(
        Guid departmentNodeId,
        DepartmentCode departmentCode,
        CancellationToken cancellationToken)
    {
        var users = await _departmentUserRepository.GetQueryableAsync();
        var existingEmails = await users
            .Where(item => item.DepartmentNodeId == departmentNodeId)
            .Select(item => item.EmailAddress)
            .ToListAsync(cancellationToken);

        foreach (var user in GetDemoUsers(departmentNodeId, departmentCode))
        {
            if (existingEmails.Contains(user.EmailAddress))
            {
                continue;
            }

            await _departmentUserRepository.InsertAsync(user, autoSave: true, cancellationToken);
        }
    }

    private async Task SeedDemoCitizensAsync(Guid localDepartmentNodeId, CancellationToken cancellationToken)
    {
        var citizens = await _citizenRepository.GetQueryableAsync();
        var existingNationalIds = await citizens
            .Where(item => item.DepartmentNodeId == localDepartmentNodeId)
            .Select(item => item.NationalIdNumber)
            .ToListAsync(cancellationToken);

        foreach (var citizen in GetDemoCitizens(localDepartmentNodeId))
        {
            if (existingNationalIds.Contains(citizen.NationalIdNumber))
            {
                continue;
            }

            await _citizenRepository.InsertAsync(citizen, autoSave: true, cancellationToken);
        }
    }

    private string GetDemoApiBaseUrl(DepartmentCode departmentCode)
    {
        if (departmentCode == _nodeOptions.DepartmentCode)
        {
            return _nodeOptions.ApiBaseUrl;
        }

        var configuredPeer = _nodeOptions.Peers.SingleOrDefault(item => item.DepartmentCode == departmentCode);
        if (configuredPeer is not null)
        {
            return configuredPeer.ApiBaseUrl;
        }

        return departmentCode switch
        {
            DepartmentCode.HomeAffairs => "http://localhost:5076",
            DepartmentCode.Sars => "http://localhost:5077",
            DepartmentCode.Municipality => "http://localhost:5078",
            DepartmentCode.Health => "http://localhost:5079",
            _ => "http://localhost:5080"
        };
    }

    private IReadOnlyCollection<DepartmentCode> GetDemoDepartments()
    {
        var departments = new List<DepartmentCode>
        {
            DepartmentCode.HomeAffairs,
            DepartmentCode.Sars,
            DepartmentCode.Municipality,
            DepartmentCode.Health
        };

        if (!departments.Contains(_nodeOptions.DepartmentCode))
        {
            departments.Add(_nodeOptions.DepartmentCode);
        }

        return departments;
    }

    private static IEnumerable<DepartmentUser> GetDemoUsers(Guid departmentNodeId, DepartmentCode departmentCode)
    {
        return departmentCode switch
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
            DepartmentCode.Health =>
            [
                new DepartmentUser(departmentNodeId, "Dr Nomsa Mabena", "Public Health Verifier", "nomsa.mabena@health.gov.za"),
                new DepartmentUser(departmentNodeId, "Karabo Molefe", "Health Records Supervisor", "karabo.molefe@health.gov.za")
            ],
            _ =>
            [
                new DepartmentUser(departmentNodeId, "Demo Approver", "Department Approver", "approver@civicsync.local"),
                new DepartmentUser(departmentNodeId, "Demo Supervisor", "Department Supervisor", "supervisor@civicsync.local")
            ]
        };
    }

    private static IEnumerable<Citizen> GetDemoCitizens(Guid localDepartmentNodeId)
    {
        return
        [
            new Citizen(
                localDepartmentNodeId,
                "0008289830183",
                new PersonName("Mike", "Johnson"),
                new ContactDetails("mike.johnson@example.com", "+27821234567")),
            new Citizen(
                localDepartmentNodeId,
                "9001015009087",
                new PersonName("Thandi", "Mokoena"),
                new ContactDetails("thandi.mokoena@example.com", "+27827654321"))
        ];
    }
}
