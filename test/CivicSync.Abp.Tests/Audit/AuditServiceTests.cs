using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.ChangeRequests;
using CivicSync.Node.Api.Tests.TestSupport;
using System.Net;
using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Audit;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Ledger;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.Sync;
using CivicSync.Node.Api.Domain.ValueObjects;
using CivicSync.Node.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Audit;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task GetLedgerEntriesAsync_ReturnsLedgerEntriesInNewestFirstOrder()
    {
        await using var dbContext = CreateDbContext();
        var olderEntry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            LedgerEventType.ChangeCommitted,
            new RecordProof("older-payload"),
            new RecordProof("GENESIS"),
            new RecordProof("older-current"))
        {
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        var newerEntry = new LedgerEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            LedgerEventType.ReplicaSynced,
            new RecordProof("newer-payload"),
            new RecordProof("older-current"),
            new RecordProof("newer-current"))
        {
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.LedgerEntries.AddRange(olderEntry, newerEntry);
        await Task.CompletedTask;

        var service = CreateService(dbContext, new NodeOptions(), new StaticHttpMessageHandler(HttpStatusCode.OK));

        var entries = await service.GetLedgerEntriesAsync();

        Assert.Collection(
            entries,
            first => Assert.Equal(newerEntry.Id, first.Id),
            second => Assert.Equal(olderEntry.Id, second.Id));
    }

    [Fact]
    public async Task GetOutboxEventsAsync_ReturnsRetryAndStatusFields()
    {
        await using var dbContext = CreateDbContext();
        var node = new DepartmentNode(DepartmentCode.HomeAffairs, "http://localhost:5076");
        var outboxEvent = new SyncOutboxEvent(node.Id, Guid.NewGuid())
        {
            Status = SyncStatus.Failed,
            RetryCount = 2
        };
        dbContext.DepartmentNodes.Add(node);
        dbContext.SyncOutboxEvents.Add(outboxEvent);
        await Task.CompletedTask;

        var service = CreateService(dbContext, new NodeOptions(), new StaticHttpMessageHandler(HttpStatusCode.OK));

        var outboxEvents = await service.GetOutboxEventsAsync();
        var dto = Assert.Single(outboxEvents);

        Assert.Equal(outboxEvent.Id, dto.Id);
        Assert.Equal(SyncStatus.Failed, dto.Status);
        Assert.Equal(2, dto.RetryCount);
    }

    [Fact]
    public async Task GetPeerHealthAsync_ReturnsOnlinePeer_WhenNodeEndpointRespondsSuccessfully()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                Peers =
                [
                    new PeerNodeOptions
                    {
                        DepartmentCode = DepartmentCode.Sars,
                        ApiBaseUrl = "http://localhost:5077"
                    }
                ]
            },
            new StaticHttpMessageHandler(HttpStatusCode.OK));

        var peers = await service.GetPeerHealthAsync();
        var peer = Assert.Single(peers);

        Assert.Equal(DepartmentCode.Sars, peer.DepartmentCode);
        Assert.True(peer.IsOnline);
    }

    [Fact]
    public async Task GetPeerHealthAsync_ReturnsOfflinePeer_WhenNodeEndpointFails()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(
            dbContext,
            new NodeOptions
            {
                Peers =
                [
                    new PeerNodeOptions
                    {
                        DepartmentCode = DepartmentCode.Sars,
                        ApiBaseUrl = "http://localhost:5077"
                    }
                ]
            },
            new StaticHttpMessageHandler(HttpStatusCode.ServiceUnavailable));

        var peers = await service.GetPeerHealthAsync();
        var peer = Assert.Single(peers);

        Assert.Equal(DepartmentCode.Sars, peer.DepartmentCode);
        Assert.False(peer.IsOnline);
    }

    private static AuditService CreateService(
        CivicSyncDbContext dbContext,
        NodeOptions nodeOptions,
        HttpMessageHandler handler)
    {
        return new AuditService(
            new TestRepository<LedgerEntry>(dbContext),
            new TestRepository<NodeSyncReceipt>(dbContext),
            new TestRepository<SyncInboxEntry>(dbContext),
            new TestRepository<SyncOutboxEvent>(dbContext),
            Options.Create(nodeOptions),
            new HttpClient(handler));
    }

    private static CivicSyncDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CivicSyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CivicSyncDbContext(options);
    }

    private sealed class StaticHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public StaticHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}



