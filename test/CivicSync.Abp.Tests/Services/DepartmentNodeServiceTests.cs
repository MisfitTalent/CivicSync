using CivicSync.Core.Configuration;
using CivicSync.Application.Services.Nodes;
using CivicSync.Application.Contracts.Nodes;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Services;

public sealed class DepartmentNodeServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesDepartmentNodeAndRegistersPeerOnCurrentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        dbContext.DepartmentNodes.Add(homeAffairs);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var request = new RegisterDepartmentNodeRequest
        {
            DepartmentCode = DepartmentCode.Safety,
            ApiBaseUrl = "http://localhost:5080/",
            RegisterAsPeerOfCurrentNode = true
        };

        var result = await service.RegisterAsync(request);

        Assert.Equal(DepartmentCode.Safety, result.DepartmentCode);
        Assert.Equal("http://localhost:5080", result.ApiBaseUrl);
        Assert.Equal(NodeStatus.Online, result.Status);
        var peer = Assert.Single(homeAffairs.KnownPeers);
        Assert.Equal(DepartmentCode.Safety, peer.PeerDepartmentCode);
        Assert.Equal("http://localhost:5080", peer.PeerBaseUrl);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenDepartmentNodeAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.DepartmentNodes.Add(new DepartmentNode(DepartmentCode.Health, "http://localhost:5079"));
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var request = new RegisterDepartmentNodeRequest
        {
            DepartmentCode = DepartmentCode.Health,
            ApiBaseUrl = "http://localhost:5080"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));

        Assert.Equal("Department node is already registered.", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenApiBaseUrlIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var request = new RegisterDepartmentNodeRequest
        {
            DepartmentCode = DepartmentCode.Safety,
            ApiBaseUrl = "not-a-url"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(request));

        Assert.Equal("Department API base URL must be an absolute HTTP or HTTPS URL.", exception.Message);
    }

    [Fact]
    public async Task GetRegisteredNodesAsync_ReturnsNodesWithKnownPeers()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        homeAffairs.RegisterPeer(DepartmentCode.Sars, "http://localhost:5077");
        dbContext.DepartmentNodes.Add(homeAffairs);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var result = await service.GetRegisteredNodesAsync();
        var node = Assert.Single(result);
        var peer = Assert.Single(node.KnownPeers);

        Assert.Equal(DepartmentCode.HomeAffairs, node.DepartmentCode);
        Assert.Equal(DepartmentCode.Sars, peer.DepartmentCode);
        Assert.Equal("http://localhost:5077", peer.ApiBaseUrl);
    }

    private static DepartmentNodeService CreateService(CivicSyncDbContext dbContext, DepartmentCode departmentCode)
    {
        return new DepartmentNodeService(
            new TestRepository<DepartmentNode>(dbContext),
            Options.Create(new NodeOptions
            {
                DepartmentCode = departmentCode
            }));
    }
}
