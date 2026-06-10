namespace CivicSync.Application.Contracts.Ledger;

public sealed class ChangeRequestProcessingFailureDto
{
    public Guid ChangeRequestId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
