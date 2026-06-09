namespace CivicSync.Application.Contracts.Sync;

public sealed class ApplyPendingInboxResponse
{
    public int ProcessedInboxEntries { get; set; }
    public int AppliedInboxEntries { get; set; }
    public int StillQueuedInboxEntries { get; set; }
    public IReadOnlyCollection<SynchronizedLedgerEntryResponse> Results { get; set; } = [];
}
