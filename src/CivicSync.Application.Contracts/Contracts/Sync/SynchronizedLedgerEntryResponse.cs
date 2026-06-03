using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Sync;

public sealed class SynchronizedLedgerEntryResponse
{
    public Guid LedgerEntryId { get; set; }
    public SyncResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
}
