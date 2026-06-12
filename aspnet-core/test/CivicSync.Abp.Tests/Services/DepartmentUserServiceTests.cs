using CivicSync.Core.Configuration;
using CivicSync.Application.Services.Nodes;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Services;

public sealed class DepartmentUserServiceTests
{
    [Fact]
    public async Task GetCurrentNodeUsersAsync_ReturnsOnlyCurrentDepartmentUsers()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.DepartmentUsers.AddRange(
            new DepartmentUser(homeAffairs.Id, "Naledi Mokoena", "Senior Identity Verifier", "naledi.mokoena@homeaffairs.gov.za"),
            new DepartmentUser(sars.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za"));
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var users = await service.GetCurrentNodeUsersAsync();
        var user = Assert.Single(users);

        Assert.Equal(homeAffairs.Id, user.DepartmentNodeId);
        Assert.Equal("Naledi Mokoena", user.FullName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenUserBelongsToCurrentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.Municipality, "http://localhost:5078");
        var departmentUser = new DepartmentUser(node.Id, "Ayesha Patel", "Municipal Records Officer", "ayesha.patel@municipality.gov.za");
        dbContext.DepartmentNodes.Add(node);
        dbContext.DepartmentUsers.Add(departmentUser);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.Municipality);

        var result = await service.GetByIdAsync(departmentUser.Id);

        Assert.Equal(departmentUser.Id, result.Id);
        Assert.Equal("Municipal Records Officer", result.Role);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenUserBelongsToDifferentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var sarsUser = new DepartmentUser(sars.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za");
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.DepartmentUsers.Add(sarsUser);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetByIdAsync(sarsUser.Id));

        Assert.Equal("Department user was not found.", exception.Message);
    }

    private static DepartmentUserService CreateService(CivicSyncDbContext dbContext, DepartmentCode departmentCode)
    {
        return new DepartmentUserService(
            new TestRepository<DepartmentNode>(dbContext),
            new TestRepository<DepartmentUser>(dbContext),
            Options.Create(new NodeOptions
            {
                DepartmentCode = departmentCode
            }));
    }
}
