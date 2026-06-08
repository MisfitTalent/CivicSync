using CivicSync.Node.Api.Tests.TestSupport;
using System.Net;
using System.Text.Json;
using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Sync;
using CivicSync.Node.Api.Contracts.Sync;
using CivicSync.Node.Api.Domain.ChangeRequests;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Ledger;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.Sync;
using CivicSync.Node.Api.Domain.ValueObjects;
using CivicSync.Node.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Sync;

public sealed class SyncServiceTests
{
    [Fact]
    public async Task PublishPendingOutboxEventsAsync_SkipsFailedOutbox_WhenRetryLimitIsReached()
    {
        await using var dbContext = CreateDbContext();
        var localNode = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var outboxEvent = new SyncOutboxEvent(localNode.Id, Guid.NewGuid())
        {
            Status = SyncStatus.Failed,
            RetryCount = 3
        };

        dbContext.DepartmentNodes.Add(localNode);
        dbContext.SyncOutboxEvents.Add(outboxEvent);
        await Task.CompletedTask;

        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                DepartmentCode = DepartmentCode.HomeAffairs,
                MaxSyncPublishAttempts = 3
            },
            new StubHttpMessageHandler(HttpStatusCode.OK, new SynchronizedLedgerEntryResponse()));

        var response = await service.PublishPendingOutboxEventsAsync();

        Assert.Equal(0, response.ProcessedOutboxEvents);
        Assert.Equal(1, response.SkippedOutboxEvents);
        Assert.Equal(SyncStatus.Failed, outboxEvent.Status);
        Assert.Equal(3, outboxEvent.RetryCount);
    }

    [Fact]
    public async Task PublishPendingOutboxEventsAsync_SignsPeerRequestAndMarksOutboxPublished_WhenPeerAppliesLedger()
    {
        await using var dbContext = CreateDbContext();
        var localNode = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        localNode.RegisterPeer(DepartmentCode.Sars, "http://localhost:5077");
        var citizen = new Citizen(
            localNode.Id,
            "9001015009087",
            new PersonName("Test", "Citizen"),
            new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = new ChangeRequest(localNode.Id, citizen.Id, "Update contact details", citizen.RecordVersion);
        changeRequest.AddFieldChange("ContactDetails", "old@example.test|+27000000000", "new@example.test|+27820000000");
        var ledgerEntry = new LedgerEntry(
            localNode.Id,
            changeRequest.Id,
            1,
            LedgerEventType.ChangeCommitted,
            new RecordProof("payload-proof"),
            new RecordProof("GENESIS"),
            new RecordProof("current-proof"));
        var outboxEvent = new SyncOutboxEvent(localNode.Id, ledgerEntry.Id);
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.OK,
            new SynchronizedLedgerEntryResponse
            {
                LedgerEntryId = ledgerEntry.Id,
                Result = SyncResult.Applied,
                Message = "Applied"
            });

        dbContext.DepartmentNodes.Add(localNode);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        dbContext.LedgerEntries.Add(ledgerEntry);
        dbContext.SyncOutboxEvents.Add(outboxEvent);
        await Task.CompletedTask;

        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                DepartmentCode = DepartmentCode.HomeAffairs,
                MaxSyncPublishAttempts = 3,
                Peers =
                [
                    new PeerNodeOptions
                    {
                        DepartmentCode = DepartmentCode.Sars,
                        ApiBaseUrl = "http://localhost:5077",
                        SharedSecret = "peer-secret"
                    }
                ]
            },
            handler);

        var response = await service.PublishPendingOutboxEventsAsync();

        Assert.Equal(1, response.ProcessedOutboxEvents);
        Assert.Equal(0, response.FailedPeerDeliveries);
        Assert.Equal(1, response.SuccessfulPeerDeliveries);
        Assert.Equal(SyncStatus.Published, outboxEvent.Status);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://localhost:5077/api/sync/ledger-entries", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("X-CivicSync-Node"));
        Assert.True(handler.LastRequest.Headers.Contains("X-CivicSync-Timestamp"));
        Assert.True(handler.LastRequest.Headers.Contains("X-CivicSync-Signature"));
    }

    [Fact]
    public async Task PublishPendingOutboxEventsAsync_PublishesFailedOutbox_WhenPeerComesBackOnline()
    {
        await using var dbContext = CreateDbContext();
        var localNode = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        localNode.RegisterPeer(DepartmentCode.Sars, "http://localhost:5077");
        var citizen = new Citizen(
            localNode.Id,
            "9001015009087",
            new PersonName("Retry", "Citizen"),
            new ContactDetails("old@example.test", "+27000000000"));
        var changeRequest = new ChangeRequest(localNode.Id, citizen.Id, "Retry sync after peer recovery", citizen.RecordVersion);
        changeRequest.AddFieldChange("ContactDetails", "old@example.test|+27000000000", "new@example.test|+27820000000");
        var ledgerEntry = new LedgerEntry(
            localNode.Id,
            changeRequest.Id,
            1,
            LedgerEventType.ChangeCommitted,
            new RecordProof("payload-proof"),
            new RecordProof("GENESIS"),
            new RecordProof("current-proof"));
        var outboxEvent = new SyncOutboxEvent(localNode.Id, ledgerEntry.Id);
        var handler = new SequenceHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent(new SynchronizedLedgerEntryResponse
                {
                    LedgerEntryId = ledgerEntry.Id,
                    Result = SyncResult.Applied,
                    Message = "Applied after peer came back online"
                })
            });

        dbContext.DepartmentNodes.Add(localNode);
        dbContext.Citizens.Add(citizen);
        dbContext.ChangeRequests.Add(changeRequest);
        dbContext.LedgerEntries.Add(ledgerEntry);
        dbContext.SyncOutboxEvents.Add(outboxEvent);
        await Task.CompletedTask;

        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                DepartmentCode = DepartmentCode.HomeAffairs,
                MaxSyncPublishAttempts = 3,
                Peers =
                [
                    new PeerNodeOptions
                    {
                        DepartmentCode = DepartmentCode.Sars,
                        ApiBaseUrl = "http://localhost:5077",
                        SharedSecret = "peer-secret"
                    }
                ]
            },
            handler);

        var firstPublish = await service.PublishPendingOutboxEventsAsync();
        var secondPublish = await service.PublishPendingOutboxEventsAsync();

        Assert.Equal(1, firstPublish.ProcessedOutboxEvents);
        Assert.Equal(1, firstPublish.FailedPeerDeliveries);
        Assert.Equal(SyncStatus.Published, outboxEvent.Status);
        Assert.Equal(1, outboxEvent.RetryCount);
        Assert.Equal(1, secondPublish.ProcessedOutboxEvents);
        Assert.Equal(1, secondPublish.SuccessfulPeerDeliveries);
        Assert.Equal(0, secondPublish.FailedPeerDeliveries);
        Assert.Equal(2, handler.RequestCount);
        Assert.Single(dbContext.NodeSyncReceipts.Local);
    }

    [Fact]
    public async Task ApplyPendingInboxEntriesAsync_AppliesQueuedLedger_WhenCitizenIsCreatedLater()
    {
        await using var dbContext = CreateDbContext();
        var localNode = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var request = new ReceiveLedgerEntryRequest
        {
            LedgerEntryId = Guid.NewGuid(),
            OriginatingNodeId = Guid.NewGuid(),
            ChangeRequestId = Guid.NewGuid(),
            SequenceNumber = 7,
            EventType = LedgerEventType.ChangeCommitted,
            PayloadProofHash = "payload-proof",
            PreviousProofHash = "previous-proof",
            CurrentProofHash = "current-proof",
            CitizenNationalIdNumber = "0008289830183",
            FieldChanges =
            [
                new SyncedFieldChangeDto
                {
                    FieldName = "ContactDetails",
                    NewValue = "newemail@example.com|0821234567"
                }
            ]
        };
        dbContext.DepartmentNodes.Add(localNode);
        await Task.CompletedTask;
        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                DepartmentCode = DepartmentCode.Sars
            },
            new StubHttpMessageHandler(HttpStatusCode.OK, new SynchronizedLedgerEntryResponse()));

        var receiveResponse = await service.ReceiveLedgerEntryAsync(request);

        Assert.Equal(SyncResult.Queued, receiveResponse.Result);
        var queuedInboxEntry = Assert.Single(dbContext.SyncInboxEntries.Local);
        Assert.Equal(SyncStatus.Received, queuedInboxEntry.Status);

        var citizen = new Citizen(
            localNode.Id,
            "0008289830183",
            new PersonName("Mike", "Johson"),
            new ContactDetails("who@where.com", "0987654321"));
        dbContext.Citizens.Add(citizen);
        await Task.CompletedTask;

        var applyResponse = await service.ApplyPendingInboxEntriesAsync();

        Assert.Equal(1, applyResponse.ProcessedInboxEntries);
        Assert.Equal(1, applyResponse.AppliedInboxEntries);
        Assert.Equal(0, applyResponse.StillQueuedInboxEntries);
        Assert.Equal(SyncStatus.Applied, queuedInboxEntry.Status);
        Assert.Equal("newemail@example.com", citizen.ContactDetails.EmailAddress);
        Assert.Equal("0821234567", citizen.ContactDetails.PhoneNumber);
    }

    [Fact]
    public async Task ReceiveLedgerEntryAsync_CreatesCitizenReplica_WhenSnapshotIsProvided()
    {
        await using var dbContext = CreateDbContext();
        var localNode = new DepartmentNode(DepartmentCode.Sars, "http://localhost:5077");
        var request = new ReceiveLedgerEntryRequest
        {
            LedgerEntryId = Guid.NewGuid(),
            OriginatingNodeId = Guid.NewGuid(),
            ChangeRequestId = Guid.NewGuid(),
            SequenceNumber = 8,
            EventType = LedgerEventType.ChangeCommitted,
            PayloadProofHash = "payload-proof",
            PreviousProofHash = "previous-proof",
            CurrentProofHash = "current-proof",
            CitizenNationalIdNumber = "8811053466666",
            CitizenFirstName = "Smoke",
            CitizenLastName = "Tester",
            CitizenEmailAddress = "smoke.updated@example.com",
            CitizenPhoneNumber = "+27829999999",
            FieldChanges =
            [
                new SyncedFieldChangeDto
                {
                    FieldName = "ContactDetails",
                    NewValue = "smoke.updated@example.com|+27829999999"
                }
            ]
        };
        dbContext.DepartmentNodes.Add(localNode);
        await Task.CompletedTask;
        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                DepartmentCode = DepartmentCode.Sars
            },
            new StubHttpMessageHandler(HttpStatusCode.OK, new SynchronizedLedgerEntryResponse()));

        var response = await service.ReceiveLedgerEntryAsync(request);

        Assert.Equal(SyncResult.Applied, response.Result);
        var citizen = Assert.Single(dbContext.Citizens.Local);
        Assert.Equal(localNode.Id, citizen.DepartmentNodeId);
        Assert.Equal("8811053466666", citizen.NationalIdNumber);
        Assert.Equal("Smoke", citizen.FullName.FirstName);
        Assert.Equal("Tester", citizen.FullName.LastName);
        Assert.Equal("smoke.updated@example.com", citizen.ContactDetails.EmailAddress);
        Assert.Equal("+27829999999", citizen.ContactDetails.PhoneNumber);
        Assert.Equal(1, citizen.RecordVersion);
        var inboxEntry = Assert.Single(dbContext.SyncInboxEntries.Local);
        Assert.Equal(SyncStatus.Applied, inboxEntry.Status);
    }

    private static SyncService CreateService(
        CivicSyncDbContext dbContext,
        NodeOptions nodeOptions,
        HttpMessageHandler handler)
    {
        return new SyncService(
            new TestRepository<ChangeRequest>(dbContext),
            new TestRepository<Citizen>(dbContext),
            new TestRepository<DepartmentNode>(dbContext),
            new TestRepository<LedgerEntry>(dbContext),
            new TestRepository<NodeSyncReceipt>(dbContext),
            new TestRepository<SyncInboxEntry>(dbContext),
            new TestRepository<SyncOutboxEvent>(dbContext),
            Options.Create(nodeOptions),
            new HttpClient(handler),
            new NodeSyncSignatureService());
    }

    private static CivicSyncDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CivicSyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CivicSyncDbContext(options);
    }

    private static StringContent JsonContent(SynchronizedLedgerEntryResponse response)
    {
        return new StringContent(JsonSerializer.Serialize(response));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly SynchronizedLedgerEntryResponse _response;

        public StubHttpMessageHandler(HttpStatusCode statusCode, SynchronizedLedgerEntryResponse response)
        {
            _statusCode = statusCode;
            _response = response;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            var json = JsonSerializer.Serialize(_response);

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(json)
            });
        }
    }

    private sealed class SequenceHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            return Task.FromResult(_responses.Dequeue());
        }
    }
}




