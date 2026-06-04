namespace CivicSync.Node.Api.Contracts.Ledger;

public sealed class ProcessApprovedChangeRequestsRequest
{
    public int MaxItems { get; set; } = 10;
}
