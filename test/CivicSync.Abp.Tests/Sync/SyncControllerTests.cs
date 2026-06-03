using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Application.Services.Sync;
using CivicSync.Node.Api.Contracts.Sync;
using CivicSync.Node.Api.Controllers;
using CivicSync.Node.Api.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicSync.Node.Api.Tests.Sync;

public sealed class SyncControllerTests
{
    [Fact]
    public async Task ReceiveLedgerEntryAsync_ReturnsUnauthorized_WhenSignatureHeadersAreMissing()
    {
        var controller = new SyncController(
            new StubSyncService(),
            new NodeSyncSignatureService(),
            Options.Create(new NodeOptions()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.ReceiveLedgerEntryAsync(CreateRequest(), CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<SynchronizedLedgerEntryResponse>(unauthorizedResult.Value);
        Assert.Equal(SyncResult.Rejected, response.Result);
    }

    private static ReceiveLedgerEntryRequest CreateRequest()
    {
        return new ReceiveLedgerEntryRequest
        {
            LedgerEntryId = Guid.NewGuid(),
            OriginatingNodeId = Guid.NewGuid(),
            ChangeRequestId = Guid.NewGuid(),
            SequenceNumber = 1,
            EventType = LedgerEventType.ChangeCommitted,
            PayloadProofHash = "payload",
            PreviousProofHash = "previous",
            CurrentProofHash = "current",
            CitizenNationalIdNumber = "9001015009087",
            FieldChanges =
            [
                new SyncedFieldChangeDto
                {
                    FieldName = "ContactDetails",
                    NewValue = "valid@example.test|+27820000000"
                }
            ]
        };
    }

    private sealed class StubSyncService : ISyncService
    {
        public Task<SynchronizedLedgerEntryResponse> ReceiveLedgerEntryAsync(
            ReceiveLedgerEntryRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The service should not be called when signature validation fails.");
        }

        public Task<PublishOutboxResponse> PublishPendingOutboxEventsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PublishOutboxResponse());
        }

        public Task<ApplyPendingInboxResponse> ApplyPendingInboxEntriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ApplyPendingInboxResponse());
        }
    }
}

