using CivicSync.Node.Api.Contracts.Audit;

namespace CivicSync.Node.Api.Application.Services.Audit;

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditLedgerEntryDto>> GetLedgerEntriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditSyncOutboxEventDto>> GetOutboxEventsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditSyncInboxEntryDto>> GetInboxEntriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditNodeSyncReceiptDto>> GetSyncReceiptsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PeerHealthDto>> GetPeerHealthAsync(CancellationToken cancellationToken = default);
}
