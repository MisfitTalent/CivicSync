using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.ChangeRequests;
using CivicSync.Node.Api.Contracts.ChangeRequests;
using CivicSync.Node.Api.Domain.ChangeRequests;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.ValueObjects;
using CivicSync.Node.Api.Infrastructure.Persistence;
using CivicSync.Node.Api.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Services;

public sealed class ChangeRequestServiceTests
{
    [Fact]
    public async Task SubmitAsync_CreatesDraftChangeRequest_WithOldAndNewFieldValues()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);

        var result = await service.SubmitAsync(new SubmitChangeRequest
        {
            CitizenId = citizen.Id,
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

        var fieldChange = Assert.Single(result.FieldChanges);
        Assert.Equal(ChangeRequestStatus.Draft, result.Status);
        Assert.Equal("old@example.test|+27000000000", fieldChange.OldValue);
        Assert.Equal("new@example.test|+27820000000", fieldChange.NewValue);
    }

    [Fact]
    public async Task RequestApprovalAsync_SetsStatusToPendingApproval_WithApproverDetailsFromDepartmentUser()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = CreateCitizen(node.Id);
        var approver = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.DepartmentUsers.Add(approver);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);

        var result = await service.RequestApprovalAsync(
            changeRequest.Id,
            new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = node.Id,
                ApproverUserId = approver.Id
            });

        var approval = Assert.Single(result.Approvals);
        Assert.Equal(ChangeRequestStatus.PendingApproval, result.Status);
        Assert.Equal(ApprovalDecision.Pending, approval.Decision);
        Assert.Equal(approver.Id, approval.ApproverUserId);
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
        var sarsApprover = new DepartmentUser(sars.Id, "Thabo Dlamini", "Tax Compliance Officer", "thabo.dlamini@sars.gov.za");
        var citizen = CreateCitizen(homeAffairs.Id);
        dbContext.DepartmentNodes.AddRange(homeAffairs, sars);
        dbContext.DepartmentUsers.Add(sarsApprover);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);

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
        var approver = new DepartmentUser(node.Id, "Naledi Mokoena", "Senior Verifier", "naledi.mokoena@homeaffairs.gov.za");
        approver.Deactivate();
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.DepartmentUsers.Add(approver);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestApprovalAsync(changeRequest.Id, new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = node.Id,
                ApproverUserId = approver.Id
            }));

        Assert.Equal("Approver user is inactive.", exception.Message);
    }

    [Fact]
    public async Task RequestApprovalAsync_Throws_WhenApproverDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = CreateCitizen(node.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RequestApprovalAsync(changeRequest.Id, new RequestDepartmentApprovalRequest
            {
                ApprovingNodeId = node.Id,
                ApproverUserId = Guid.NewGuid()
            }));

        Assert.Equal("Approver user does not exist.", exception.Message);
    }

    [Fact]
    public async Task RecordDecisionAsync_SetsStatusToApproved_WhenApprovalIsApproved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, setup.Citizen.Id);
        await RequestApprovalAsync(service, changeRequest.Id, setup.Node.Id, setup.Approver.Id);

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
    public async Task RecordDecisionAsync_SetsStatusToRejected_WhenApprovalIsRejected()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var setup = AddNodeCitizenAndApprover(dbContext);
        await Task.CompletedTask;
        var service = CreateService(dbContext, DepartmentCode.HomeAffairs);
        var changeRequest = await SubmitContactChangeAsync(service, setup.Citizen.Id);
        await RequestApprovalAsync(service, changeRequest.Id, setup.Node.Id, setup.Approver.Id);

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
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);
        await RequestApprovalAsync(service, changeRequest.Id, node.Id, approver.Id);

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
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);
        await RequestApprovalAsync(service, changeRequest.Id, node.Id, assignedApprover.Id);

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
        var changeRequest = await SubmitContactChangeAsync(service, citizen.Id);
        await RequestApprovalAsync(service, changeRequest.Id, node.Id, approver.Id);
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
            new ContactDetails("old@example.test", "+27000000000"));
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

    private static Task<ChangeRequestDto> RequestApprovalAsync(
        ChangeRequestService service,
        Guid changeRequestId,
        Guid nodeId,
        Guid approverId)
    {
        return service.RequestApprovalAsync(changeRequestId, new RequestDepartmentApprovalRequest
        {
            ApprovingNodeId = nodeId,
            ApproverUserId = approverId
        });
    }
}
