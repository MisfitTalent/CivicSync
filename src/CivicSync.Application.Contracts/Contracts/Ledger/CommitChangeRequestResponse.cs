namespace CivicSync.Node.Api.Contracts.Ledger;

public sealed class CommitChangeRequestResponse
{
    public Guid ChangeRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public LedgerEntryDto LedgerEntry { get; set; } = new();
}
