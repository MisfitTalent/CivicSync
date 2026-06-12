using CivicSync.Core.Configuration;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Seed;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Infrastructure;

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
        var seededCitizen = Assert.Single(dbContext.Citizens.Local, item => item.NationalIdNumber == "0008289830183");
        Assert.Equal("28 August 2000", seededCitizen.DateOfBirth);
        Assert.Equal("M12345678", seededCitizen.PassportNumber);
        Assert.Equal("9876543210", seededCitizen.TaxNumber);
        Assert.Equal("14 Ubuntu Street, Soweto, 1804", seededCitizen.ResidentialAddress);
        Assert.Equal("MUN-2024-88821", seededCitizen.RatesAccount);
        Assert.Contains("IRP5", seededCitizen.EmploymentHistory);
        Assert.Contains("investment returns", seededCitizen.IncomeAndInvestmentProfile);
        Assert.Contains("Bank interest certificates", seededCitizen.BankingAndAssets);
        Assert.All(dbContext.Citizens.Local, item => Assert.Equal(1, item.RecordVersion));
    }
}
