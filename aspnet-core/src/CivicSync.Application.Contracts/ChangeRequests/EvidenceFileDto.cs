namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class EvidenceFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}
