using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Sync;

public sealed class SynchronizedLedgerEntryResponse
{
    public Guid LedgerEntryId { get; set; }
    public SyncResult Result { get; set; }
    public string Message { get; set; } = string.Empty;
}
