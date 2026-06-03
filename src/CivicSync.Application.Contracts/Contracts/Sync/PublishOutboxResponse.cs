namespace CivicSync.Node.Api.Contracts.Sync;

public sealed class PublishOutboxResponse
{
    public int ProcessedOutboxEvents { get; set; }
    public int SkippedOutboxEvents { get; set; }
    public int SuccessfulPeerDeliveries { get; set; }
    public int FailedPeerDeliveries { get; set; }
    public IReadOnlyCollection<PeerSyncResultDto> PeerResults { get; set; } = [];
}
