using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Infrastructure.Persistence.Seed;
using CivicSync.Node.Api.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Infrastructure;

public sealed class NodeDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesDemoNodesUsersAndCitizens_Idempotently()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seeder = new NodeDataSeeder(
            new TestRepository<DepartmentNode>(dbContext),
            new TestRepository<DepartmentUser>(dbContext),
            new TestRepository<Citizen>(dbContext),
            Options.Create(new NodeOptions
            {
                DepartmentCode = DepartmentCode.HomeAffairs,
                ApiBaseUrl = "http://localhost:5076",
                Peers =
                [
                    new PeerNodeOptions { DepartmentCode = DepartmentCode.Sars, ApiBaseUrl = "http://localhost:5077" },
                    new PeerNodeOptions { DepartmentCode = DepartmentCode.Municipality, ApiBaseUrl = "http://localhost:5078" }
                ]
            }));

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(4, dbContext.DepartmentNodes.Local.Count);
        Assert.Contains(dbContext.DepartmentNodes.Local, item => item.DepartmentCode == DepartmentCode.Health);
        Assert.Equal(8, dbContext.DepartmentUsers.Local.Count);
        Assert.Equal(2, dbContext.Citizens.Local.Count);
        Assert.Contains(dbContext.Citizens.Local, item => item.NationalIdNumber == "0008289830183");
        Assert.All(dbContext.Citizens.Local, item => Assert.Equal(1, item.RecordVersion));
    }
}
