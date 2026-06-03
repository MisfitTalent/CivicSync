using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Citizens;
using CivicSync.Node.Api.Contracts.Citizens;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.ValueObjects;
using CivicSync.Node.Api.Infrastructure.Persistence;
using CivicSync.Node.Api.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Services;

public sealed class CitizenServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesCitizenUnderCurrentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        dbContext.DepartmentNodes.Add(node);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var result = await service.CreateAsync(new CreateCitizenRequest
        {
            NationalIdNumber = "9001015009087",
            FirstName = "Test",
            LastName = "Citizen",
            EmailAddress = "test@example.com",
            PhoneNumber = "+27820000000"
        });

        Assert.Equal(node.Id, result.DepartmentNodeId);
        Assert.Equal("9001015009087", result.NationalIdNumber);
        Assert.Equal("Test Citizen", result.DisplayName);
        Assert.Equal(CitizenStatus.Active, result.Status);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNationalIdAlreadyExistsOnSameNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(new Citizen(
            node.Id,
            "9001015009087",
            new PersonName("Existing", "Citizen"),
            new ContactDetails("existing@example.com", "+27820000000")));
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateCitizenRequest
            {
                NationalIdNumber = "9001015009087",
                FirstName = "Duplicate",
                LastName = "Citizen",
                EmailAddress = "duplicate@example.com",
                PhoneNumber = "+27820000001"
            }));

        Assert.Equal("A citizen with the same national ID already exists on this node.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsSameNationalIdOnDifferentNodes()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.Citizens.Add(new Citizen(
            homeAffairs.Id,
            "9001015009087",
            new PersonName("Home", "Citizen"),
            new ContactDetails("home@example.com", "+27820000000")));
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.Sars);

        var result = await service.CreateAsync(new CreateCitizenRequest
        {
            NationalIdNumber = "9001015009087",
            FirstName = "Sars",
            LastName = "Citizen",
            EmailAddress = "sars@example.com",
            PhoneNumber = "+27820000001"
        });

        Assert.Equal(sars.Id, result.DepartmentNodeId);
        Assert.Equal(2, dbContext.Citizens.Local.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyCitizensForCurrentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.Citizens.AddRange(
            new Citizen(
                homeAffairs.Id,
                "9001015009087",
                new PersonName("Home", "Citizen"),
                new ContactDetails("home@example.com", "+27820000000")),
            new Citizen(
                sars.Id,
                "9001015009088",
                new PersonName("Sars", "Citizen"),
                new ContactDetails("sars@example.com", "+27820000001")));
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var citizens = await service.GetAllAsync();
        var citizen = Assert.Single(citizens);

        Assert.Equal(homeAffairs.Id, citizen.DepartmentNodeId);
        Assert.Equal("9001015009087", citizen.NationalIdNumber);
    }

    private static CitizenService CreateService(CivicSyncDbContext dbContext, DepartmentCode departmentCode)
    {
        return new CitizenService(
            new TestRepository<Citizen>(dbContext),
            new TestRepository<DepartmentNode>(dbContext),
            Options.Create(new NodeOptions
            {
                DepartmentCode = departmentCode
            }));
    }
}



