using CivicSync.Node.Api.Contracts.Sync;

namespace CivicSync.Node.Api.Application.Services.Sync;

public interface ISyncService
{
    Task<SynchronizedLedgerEntryResponse> ReceiveLedgerEntryAsync(
        ReceiveLedgerEntryRequest request,
        CancellationToken cancellationToken = default);

    Task<PublishOutboxResponse> PublishPendingOutboxEventsAsync(CancellationToken cancellationToken = default);

    Task<ApplyPendingInboxResponse> ApplyPendingInboxEntriesAsync(CancellationToken cancellationToken = default);
}
