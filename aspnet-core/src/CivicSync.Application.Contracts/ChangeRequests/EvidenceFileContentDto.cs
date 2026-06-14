namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class EvidenceFileContentDto
{
    public Guid Id { get; set; }
    public Guid ChangeRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
