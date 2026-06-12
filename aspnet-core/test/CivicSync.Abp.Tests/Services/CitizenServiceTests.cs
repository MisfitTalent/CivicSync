using CivicSync.Core.Configuration;
using CivicSync.Application.Services.Citizens;
using CivicSync.Application.Contracts.Citizens;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.ValueObjects;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Services;

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
            PhoneNumber = "+27820000000",
            DateOfBirth = "01 January 1990",
            PassportNumber = "A12345678",
            BiometricReference = "Fingerprint and facial scan enrolled",
            RelationshipStatus = "Civil registry relationships verified",
            TaxNumber = "9876543210",
            EmploymentHistory = "IRP5 employer payroll history available from SARS third-party submissions",
            IncomeAndInvestmentProfile = "Salary, interest, investment returns, pension and investment contributions on file",
            BankingAndAssets = "Bank interest certificates, investment portfolio data, and property deed reference on file",
            ResidentialAddress = "14 Ubuntu Street, Soweto, 1804",
            RatesAccount = "MUN-2024-88821",
            MunicipalServiceStatus = "Active municipal services"
        });

        Assert.Equal(node.Id, result.DepartmentNodeId);
        Assert.Equal("9001015009087", result.NationalIdNumber);
        Assert.Equal("Test Citizen", result.DisplayName);
        Assert.Equal(CitizenStatus.Active, result.Status);
        Assert.Equal("01 January 1990", result.DateOfBirth);
        Assert.Equal("A12345678", result.PassportNumber);
        Assert.Equal("Fingerprint and facial scan enrolled", result.BiometricReference);
        Assert.Equal("Civil registry relationships verified", result.RelationshipStatus);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.TaxNumber);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.EmploymentHistory);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.IncomeAndInvestmentProfile);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.BankingAndAssets);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.ResidentialAddress);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.RatesAccount);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.MunicipalServiceStatus);
        Assert.Contains(nameof(Citizen.TaxNumber), result.RedactedFields);
        Assert.DoesNotContain(nameof(Citizen.BiometricReference), result.RedactedFields);
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

    [Fact]
    public async Task GetByIdAsync_RedactsFieldsOutsideCurrentDepartmentAccess()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var citizen = new Citizen(
            sars.Id,
            "9001015009087",
            new PersonName("Tax", "Citizen"),
            new ContactDetails("tax@example.test", "+27820000000"))
        {
            DateOfBirth = "01 January 1990",
            PassportNumber = "A12345678",
            BiometricReference = "face-api-recognition-v1:test",
            RelationshipStatus = "Civil registry relationships verified",
            TaxNumber = "9876543210",
            EmploymentHistory = "IRP5 employer payroll history",
            IncomeAndInvestmentProfile = "Salary and investments",
            BankingAndAssets = "Bank interest certificates",
            ResidentialAddress = "14 Ubuntu Street, Soweto, 1804",
            RatesAccount = "MUN-2024-88821",
            MunicipalServiceStatus = "Active municipal services"
        };
        dbContext.DepartmentNodes.Add(sars);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.Sars);

        var result = await service.GetByIdAsync(citizen.Id);

        Assert.NotNull(result);
        Assert.Equal("9876543210", result.TaxNumber);
        Assert.Equal("14 Ubuntu Street, Soweto, 1804", result.ResidentialAddress);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.DateOfBirth);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.BiometricReference);
        Assert.Equal(CitizenFieldApprovalPolicy.RedactedValue, result.RatesAccount);
        Assert.Contains(nameof(Citizen.BiometricReference), result.RedactedFields);
        Assert.DoesNotContain(nameof(Citizen.TaxNumber), result.RedactedFields);
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



