using CivicSync.Node.Api.Domain.Sync;
using CivicSync.Node.Api.Application.Services.Ledger;
using CivicSync.Node.Api.Domain.ChangeRequests;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Ledger;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.ValueObjects;
using CivicSync.Node.Api.Tests.TestSupport;
using CivicSync.Node.Api.Infrastructure.Persistence;

namespace CivicSync.Node.Api.Tests.Services;

public sealed class LedgerServiceTests
{
    [Fact]
    public async Task CommitChangeRequestAsync_Throws_WhenChangeRequestIsNotApproved()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = new Citizen(node.Id, "9001015009087", new PersonName("Test", "Citizen"), new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = new ChangeRequest(node.Id, citizen.Id, "Update contact details", citizen.RecordVersion);
        changeRequest.AddFieldChange("ContactDetails", "old@example.test|+27000000000", "new@example.test|+27820000000");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CommitChangeRequestAsync(changeRequest.Id));

        Assert.Equal("Only approved change requests can be committed.", exception.Message);
    }

    [Fact]
    public async Task CommitChangeRequestAsync_AppliesCitizenChangeAndCreatesLedgerAndOutbox()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = new Citizen(node.Id, "9001015009087", new PersonName("Test", "Citizen"), new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = CreateApprovedContactChange(node.Id, citizen.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var result = await service.CommitChangeRequestAsync(changeRequest.Id);

        var ledgerEntry = Assert.Single(dbContext.LedgerEntries.Local);
        var outboxEvent = Assert.Single(dbContext.SyncOutboxEvents.Local);
        Assert.Equal("Committed", result.Status);
        Assert.Equal(ChangeRequestStatus.Committed, changeRequest.Status);
        Assert.Equal(2, citizen.RecordVersion);
        Assert.Equal(1, changeRequest.ExpectedCitizenVersion);
        Assert.Equal(2, changeRequest.CommittedCitizenVersion);
        Assert.Equal("new@example.test", citizen.ContactDetails.EmailAddress);
        Assert.Equal("+27820000000", citizen.ContactDetails.PhoneNumber);
        Assert.Equal(ledgerEntry.Id, outboxEvent.LedgerEntryId);
        Assert.Equal(node.Id, outboxEvent.DepartmentNodeId);
        Assert.Equal(1, ledgerEntry.SequenceNumber);
        Assert.Equal("GENESIS", ledgerEntry.PreviousProof.Hash);
        Assert.False(string.IsNullOrWhiteSpace(ledgerEntry.PayloadProof.Hash));
        Assert.False(string.IsNullOrWhiteSpace(ledgerEntry.CurrentProof.Hash));
    }

    [Fact]
    public async Task CommitChangeRequestAsync_UsesNextSequenceAndPreviousHash_WhenLedgerAlreadyExists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = new Citizen(node.Id, "9001015009087", new PersonName("Test", "Citizen"), new ContactDetails("old@example.test", "+27000000000"));
        var existingLedgerEntry = new LedgerEntry(
            node.Id,
            Guid.NewGuid(),
            9,
            LedgerEventType.ChangeCommitted,
            new RecordProof("existing-payload"),
            new RecordProof("older-current"),
            new RecordProof("existing-current"));
        var changeRequest = CreateApprovedContactChange(node.Id, citizen.Id);
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.LedgerEntries.Add(existingLedgerEntry);
        dbContext.ChangeRequests.Add(changeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var result = await service.CommitChangeRequestAsync(changeRequest.Id);

        Assert.Equal(10, result.LedgerEntry.SequenceNumber);
        Assert.Equal("existing-current", result.LedgerEntry.PreviousProofHash);
    }


    [Fact]
    public async Task CommitChangeRequestAsync_MarksConflict_WhenCitizenVersionChangedBeforeCommit()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = new Citizen(node.Id, "9001015009087", new PersonName("Test", "Citizen"), new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = CreateApprovedContactChange(node.Id, citizen.Id, citizen.RecordVersion);
        citizen.ApplySharedFieldChange("ContactDetails", "alreadychanged@example.test|+27111111111");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CommitChangeRequestAsync(changeRequest.Id));

        Assert.Equal("Citizen record version conflict. Expected version 1, but current version is 2.", exception.Message);
        Assert.Equal(ChangeRequestStatus.Conflict, changeRequest.Status);
        Assert.Empty(dbContext.LedgerEntries.Local);
        Assert.Empty(dbContext.SyncOutboxEvents.Local);
        Assert.Equal("alreadychanged@example.test", citizen.ContactDetails.EmailAddress);
    }

    private static ChangeRequest CreateApprovedContactChange(Guid nodeId, Guid citizenId, long expectedCitizenVersion = 1)
    {
        var changeRequest = new ChangeRequest(nodeId, citizenId, "Update contact details", expectedCitizenVersion);
        changeRequest.AddFieldChange("ContactDetails", "old@example.test|+27000000000", "new@example.test|+27820000000");
        changeRequest.RequestApprovalFrom(
            nodeId,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Naledi Mokoena",
            "Senior Verifier",
            "Home Affairs");
        changeRequest.RecordDecision(nodeId, ApprovalDecision.Approved, "Approved");

        return changeRequest;
    }
    private static LedgerService CreateService(CivicSyncDbContext dbContext)
    {
        return new LedgerService(
            new TestRepository<ChangeRequest>(dbContext),
            new TestRepository<Citizen>(dbContext),
            new TestRepository<LedgerEntry>(dbContext),
            new TestRepository<SyncOutboxEvent>(dbContext));
    }
}






