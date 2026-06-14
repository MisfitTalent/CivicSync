using CivicSync.Core.Domain.Sync;
using CivicSync.Application.Services.Ledger;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Ledger;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.ValueObjects;
using CivicSync.Web.Host.Tests.TestSupport;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;

namespace CivicSync.Web.Host.Tests.Services;

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

    [Fact]
    public async Task CommitChangeRequestAsync_AllowsCommit_WhenUnchangedRequestedFieldHasLaterRecordVersion()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var citizen = new Citizen(node.Id, "9001015009087", new PersonName("Test", "Citizen"), new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = CreateApprovedFullNameChange(node.Id, citizen.Id, citizen.RecordVersion);
        citizen.EnrollBiometric("Face scan", "Browser camera", "face-v1:abc123");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var result = await service.CommitChangeRequestAsync(changeRequest.Id);

        Assert.Equal("Committed", result.Status);
        Assert.Equal(ChangeRequestStatus.Committed, changeRequest.Status);
        Assert.Equal("New", citizen.FullName.FirstName);
        Assert.Equal("Name", citizen.FullName.LastName);
        Assert.Equal(3, citizen.RecordVersion);
        Assert.Single(dbContext.LedgerEntries.Local);
        Assert.Single(dbContext.SyncOutboxEvents.Local);
    }

    [Fact]
    public async Task ProcessApprovedChangeRequestsAsync_CommitsOnlyRequestedBatchSize()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var firstCitizen = new Citizen(node.Id, "9001015009087", new PersonName("First", "Citizen"), new ContactDetails("first@example.test", "+27000000001"));
        var secondCitizen = new Citizen(node.Id, "9001015009088", new PersonName("Second", "Citizen"), new ContactDetails("second@example.test", "+27000000002"));
        var firstChangeRequest = CreateApprovedContactChange(node.Id, firstCitizen.Id, firstCitizen.RecordVersion);
        var secondChangeRequest = CreateApprovedContactChange(node.Id, secondCitizen.Id, secondCitizen.RecordVersion);
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.AddRange(firstCitizen, secondCitizen);
        dbContext.ChangeRequests.AddRange(firstChangeRequest, secondChangeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var result = await service.ProcessApprovedChangeRequestsAsync(1);

        Assert.Equal(1, result.MaxItems);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.CommittedCount);
        Assert.Equal(0, result.ConflictCount);
        Assert.Equal(0, result.FailureCount);
        Assert.Single(result.CommittedChanges);
        Assert.Equal(ChangeRequestStatus.Committed, firstChangeRequest.Status);
        Assert.Equal(ChangeRequestStatus.Approved, secondChangeRequest.Status);
    }

    [Fact]
    public async Task ProcessApprovedChangeRequestsAsync_ContinuesWhenOneRequestConflicts()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var staleCitizen = new Citizen(node.Id, "9001015009087", new PersonName("Stale", "Citizen"), new ContactDetails("stale@example.test", "+27000000001"));
        var validCitizen = new Citizen(node.Id, "9001015009088", new PersonName("Valid", "Citizen"), new ContactDetails("valid@example.test", "+27000000002"));
        var staleChangeRequest = CreateApprovedContactChange(node.Id, staleCitizen.Id, staleCitizen.RecordVersion);
        var validChangeRequest = CreateApprovedContactChange(node.Id, validCitizen.Id, validCitizen.RecordVersion);
        staleCitizen.ApplySharedFieldChange("ContactDetails", "changed@example.test|+27111111111");
        dbContext.DepartmentNodes.Add(node);
        dbContext.Citizens.AddRange(staleCitizen, validCitizen);
        dbContext.ChangeRequests.AddRange(staleChangeRequest, validChangeRequest);
        await Task.CompletedTask;
        var service = CreateService(dbContext);

        var result = await service.ProcessApprovedChangeRequestsAsync(10);

        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(1, result.CommittedCount);
        Assert.Equal(1, result.ConflictCount);
        Assert.Equal(1, result.FailureCount);
        Assert.Single(result.CommittedChanges);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(staleChangeRequest.Id, failure.ChangeRequestId);
        Assert.Contains("Citizen record version conflict", failure.Reason);
        Assert.Equal(ChangeRequestStatus.Conflict, staleChangeRequest.Status);
        Assert.Equal(ChangeRequestStatus.Committed, validChangeRequest.Status);
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

    private static ChangeRequest CreateApprovedFullNameChange(Guid nodeId, Guid citizenId, long expectedCitizenVersion = 1)
    {
        var changeRequest = new ChangeRequest(nodeId, citizenId, "Update name", expectedCitizenVersion);
        changeRequest.AddFieldChange("FullName", "Test Citizen", "New Name");
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






