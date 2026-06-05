namespace CivicSync.Node.Api.Contracts.Ledger;

public sealed class ProcessApprovedChangeRequestsResponse
{
    public int MaxItems { get; set; }
    public int ProcessedCount { get; set; }
    public int CommittedCount { get; set; }
    public int ConflictCount { get; set; }
    public int FailureCount { get; set; }
    public IReadOnlyCollection<CommitChangeRequestResponse> CommittedChanges { get; set; } = [];
    public IReadOnlyCollection<ChangeRequestProcessingFailureDto> Failures { get; set; } = [];
}
