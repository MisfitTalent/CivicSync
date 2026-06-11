using CivicSync.Core.Configuration;
using CivicSync.Application.Services.ChangeRequests;
using CivicSync.Application.Contracts.ChangeRequests;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.ValueObjects;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Services;

public sealed class ChangeRequestServiceTests
{
    [Fact]
    public async Task SubmitAsync_RoutesSharedContactChangeToAllRequiredDepartments()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddCoreDepartmentApprovalSetup(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var result = await SubmitContactChangeAsync(service, setup.Citizen.Id);

        var fieldChange = Assert.Single(result.FieldChanges);
        Assert.Equal(ChangeRequestStatus.PendingApproval, result.Status);
        Assert.Equal("old@example.test|+27000000000", fieldChange.OldValue);
        Assert.Equal("new@example.test|+27820000000", fieldChange.NewValue);
        Assert.Equal(3, result.Approvals.Count);
        Assert.Contains(result.Approvals, item => item.ApprovingNodeId == setup.HomeAffairs.Id);
        Assert.Contains(result.Approvals, item => item.ApprovingNodeId == setup.Sars.Id);
        Assert.Contains(result.Approvals, item => item.ApprovingNodeId == setup.Municipality.Id);
    }

    [Fact]
    public async Task SubmitAsync_CapturesExpandedCitizenFieldOldValue_AndRoutesToOwningDepartment()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddCoreDepartmentApprovalSetup(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var result = await service.SubmitAsync(new SubmitChangeRequest
        {
            CitizenId = setup.Citizen.Id,
            Reason = "Update SARS tax number",
            FieldChanges =
            [
                new SubmitFieldChangeRequest
                {
                    FieldName = nameof(Citizen.TaxNumber),
                    NewValue = "3021456789"
                }
            ]
        });

        var fieldChange = Assert.Single(result.FieldChanges);
        var approval = Assert.Single(result.Approvals);
        Assert.Equal(ChangeRequestStatus.PendingApproval, result.Status);
        Assert.Equal(setup.Sars.Id, approval.ApprovingNodeId);
        Assert.Equal("9876543210", fieldChange.OldValue);
        Assert.Equal("3021456789", fieldChange.NewValue);
    }

    [Fact]
    public async Task RequestApprovalAsync_ReturnsExistingApproval_WhenApprovalAlreadyRequested()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, setup.Citizen.Id);

        var result = await service.RequestApprovalAsync(
            changeRequest.Id,
            new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = setup.Node.Id,
                ApproverUserId = setup.Approver.Id
            });

        var approval = Assert.Single(result.Approvals);
        Assert.Equal(ChangeRequestStatus.PendingApproval, result.Status);
        Assert.Equal(ApprovalDecision.Pending, approval.Decision);
        Assert.Equal(setup.Approver.Id, approval.ApproverUserId);
        Assert.Equal("Naledi Mokoena", approval.ApproverFullName);
        Assert.Equal("Senior Verifier", approval.ApproverRole);
        Assert.Equal("HomeAffairs", approval.ApproverDepartmentName);
    }

    [Fact]
    public async Task RequestApprovalAsync_Throws_WhenApproverBelongsToDifferentNode()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var homeAffairsApprover = new DepartmentUser(homeAffairs.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var sarsApprover = new DepartmentUser(sars.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za");
        var citizen = CreateCitizen(homeAffairs.Id);
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.DepartmentUsers.AddRange(homeAffairsApprover, sarsApprover);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestApprovalAsync(changeRequest.Id, new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = homeAffairs.Id,
                ApproverUserId = sarsApprover.Id
            }));

        Assert.Equal("Approver user does not belong to the approving node.", exception.Message);
    }

    [Fact]
    public async Task RequestApprovalAsync_Throws_WhenApproverIsInactive()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var activeApprover = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var inactiveApprover = new DepartmentUser(node.Id, "Sipho Nkosi", "Home Affairs Supervisor", "sipho.nkosi@homeaffairs.gov.za");
        inactiveApprover.Deactivate();
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.DepartmentUsers.AddRange(activeApprover, inactiveApprover);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestApprovalAsync(changeRequest.Id, new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = node.Id,
                ApproverUserId = inactiveApprover.Id
            }));

        Assert.Equal("Approver user is inactive.", exception.Message);
    }

    [Fact]
    public async Task RequestApprovalAsync_Throws_WhenApproverDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, setup.Citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestApprovalAsync(changeRequest.Id, new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = setup.Node.Id,
                ApproverUserId = Guid.NewGuid()
            }));

        Assert.Equal("Approver user does not exist.", exception.Message);
    }

    [Fact]
    public async Task RecordDecisionAsync_SetsStatusToApproved_WhenOnlyRequiredApprovalIsApproved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, setup.Citizen.Id);

        var result = await service.RecordDecisionAsync(
            changeRequest.Id,
            new RecordApprovalDecisionRequest
            {
                ApprovingNodeId = setup.Node.Id,
                ApproverUserId = setup.Approver.Id,
                Decision = ApprovalDecision.Approved,
                Comment = "Approved"
            });

        var approval = Assert.Single(result.Approvals);
        Assert.Equal(ChangeRequestStatus.Approved, result.Status);
        Assert.Equal(ApprovalDecision.Approved, approval.Decision);
    }

    [Fact]
    public async Task RecordDecisionAsync_KeepsSharedContactChangePendingUntilAllDepartmentsApprove()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddCoreDepartmentApprovalSetup(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, setup.Citizen.Id);

        var afterHomeAffairsApproval = await service.RecordDecisionAsync(
            changeRequest.Id,
            new RecordApprovalDecisionRequest
            {
                ApprovingNodeId = setup.HomeAffairs.Id,
                ApproverUserId = setup.HomeAffairsApprover.Id,
                Decision = ApprovalDecision.Approved,
                Comment = "Identity approved"
            });

        Assert.Equal(ChangeRequestStatus.PendingApproval, afterHomeAffairsApproval.Status);

        var afterSarsApproval = await service.RecordDecisionAsync(
            changeRequest.Id,
            new RecordApprovalDecisionRequest
            {
                ApprovingNodeId = setup.Sars.Id,
                ApproverUserId = setup.SarsApprover.Id,
                Decision = ApprovalDecision.Approved,
                Comment = "Tax profile approved"
            });

        Assert.Equal(ChangeRequestStatus.PendingApproval, afterSarsApproval.Status);

        var result = await service.RecordDecisionAsync(
            changeRequest.Id,
            new RecordApprovalDecisionRequest
            {
                ApprovingNodeId = setup.Municipality.Id,
                ApproverUserId = setup.MunicipalityApprover.Id,
                Decision = ApprovalDecision.Approved,
                Comment = "Municipal records approved"
            });

        Assert.Equal(ChangeRequestStatus.Approved, result.Status);
        Assert.All(result.Approvals, item => Assert.Equal(ApprovalDecision.Approved, item.Decision));
    }

    [Fact]
    public async Task RecordDecisionAsync_SetsStatusToRejected_WhenApprovalIsRejected()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, setup.Citizen.Id);

        var result = await service.RecordDecisionAsync(
            changeRequest.Id,
            new RecordApprovalDecisionRequest
            {
                ApprovingNodeId = setup.Node.Id,
                ApproverUserId = setup.Approver.Id,
                Decision = ApprovalDecision.Rejected,
                Comment = "Rejected"
            });

        Assert.Equal(ChangeRequestStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task RecordDecisionAsync_Throws_WhenNodeWasNotRequestedToApprove()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var otherNode = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var approver = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var otherNodeApprover = new DepartmentUser(otherNode.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za");
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.AddRange(node, otherNode);
        dbContext.DepartmentUsers.AddRange(approver, otherNodeApprover);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordDecisionAsync(
                changeRequest.Id,
                new RecordApprovalDecisionRequest
                {
                    ApprovingNodeId = otherNode.Id,
                    ApproverUserId = otherNodeApprover.Id,
                    Decision = ApprovalDecision.Approved
                }));

        Assert.Equal("The selected node is not required to approve this change request.", exception.Message);
    }

    [Fact]
    public async Task RecordDecisionAsync_Throws_WhenDecisionApproverIsNotAssignedApprovalUser()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var assignedApprover = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var otherApprover = new DepartmentUser(node.Id, "Sipho Nkosi", "Home Affairs Supervisor", "sipho.nkosi@homeaffairs.gov.za");
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.DepartmentUsers.AddRange(assignedApprover, otherApprover);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordDecisionAsync(
                changeRequest.Id,
                new RecordApprovalDecisionRequest
                {
                    ApprovingNodeId = node.Id,
                    ApproverUserId = otherApprover.Id,
                    Decision = ApprovalDecision.Approved
                }));

        Assert.Equal("Approver user is not assigned to this approval.", exception.Message);
    }

    [Fact]
    public async Task RecordDecisionAsync_Throws_WhenDecisionApproverIsInactive()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var approver = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.DepartmentUsers.Add(approver);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitFullNameChangeAsync(service, citizen.Id);
        approver.Deactivate();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordDecisionAsync(
                changeRequest.Id,
                new RecordApprovalDecisionRequest
                {
                    ApprovingNodeId = node.Id,
                    ApproverUserId = approver.Id,
                    Decision = ApprovalDecision.Approved
                }));

        Assert.Equal("Approver user is inactive.", exception.Message);
    }

    private static ChangeRequestService CreateService(CivicSyncDbContext dbContext, DepartmentCode departmentCode)
    {
        return new ChangeRequestService(
            new TestRepository<ChangeRequest>(dbContext),
            new TestRepository<Citizen>(dbContext),
            new TestRepository<DepartmentApproval>(dbContext),
            new TestRepository<DepartmentNode>(dbContext),
            new TestRepository<DepartmentUser>(dbContext),
            Options.Create(new NodeOptions
            {
                DepartmentCode = departmentCode
            }));
    }

    private static Citizen CreateCitizen(Guid nodeId)
    {
        return new Citizen(
            nodeId,
            "9001015009087",
            new PersonName("Test", "Citizen"),
            new ContactDetails("old@example.test", "+27000000000"))
        {
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
        };
    }

    private static (DepartmentNode Node, Citizen Citizen, DepartmentUser Approver) AddNodeCitizenAndApprover(
        CivicSyncDbContext dbContext)
    {
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = CreateCitizen(node.Id);
        var approver = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.DepartmentUsers.Add(approver);

        return (node, citizen, approver);
    }

    private static (
        DepartmentNode HomeAffairs,
        DepartmentNode Sars,
        DepartmentNode Municipality,
        Citizen Citizen,
        DepartmentUser HomeAffairsApprover,
        DepartmentUser SarsApprover,
        DepartmentUser MunicipalityApprover) AddCoreDepartmentApprovalSetup(CivicSyncDbContext dbContext)
    {
        var homeAffairs = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var sars = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var municipality = new DepartmentNode(DepartmentCode.Municipality, "http://localhost:5078");
        var citizen = CreateCitizen(homeAffairs.Id);
        var homeAffairsApprover = new DepartmentUser(homeAffairs.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        var sarsApprover = new DepartmentUser(sars.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za");
        var municipalityApprover = new DepartmentUser(municipality.Id, "Lerato Maseko", "Municipal Records Officer", "lerato.maseko@municipality.gov.za");

        dbContext.DepartmentNodes.AddRange(homeAffairs, sars, municipality);
        dbContext.Citizens.Add(citizen);
        dbContext.DepartmentUsers.AddRange(homeAffairsApprover, sarsApprover, municipalityApprover);

        return (homeAffairs, sars, municipality, citizen, homeAffairsApprover, sarsApprover, municipalityApprover);
    }

    private static Task<ChangeRequestDto> SubmitContactChangeAsync(
        ChangeRequestService service,
        Guid citizenId)
    {
        return service.SubmitAsync(new SubmitChangeRequest
        {
            CitizenId = citizenId,
            Reason = "Update contact details",
            FieldChanges =
            [
                new SubmitFieldChangeRequest
                {
                    FieldName = "ContactDetails",
                    NewValue = "new@example.test|+27820000000"
                }
            ]
        });
    }

    private static Task<ChangeRequestDto> SubmitFullNameChangeAsync(
        ChangeRequestService service,
        Guid citizenId)
    {
        return service.SubmitAsync(new SubmitChangeRequest
        {
            CitizenId = citizenId,
            Reason = "Update full name",
            FieldChanges =
            [
                new SubmitFieldChangeRequest
                {
                    FieldName = "FullName",
                    NewValue = "Updated Citizen"
                }
            ]
        });
    }
}
