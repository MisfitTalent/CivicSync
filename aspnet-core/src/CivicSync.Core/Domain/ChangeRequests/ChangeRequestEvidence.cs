using CivicSync.Core.Domain.Common;

namespace CivicSync.Core.Domain.ChangeRequests;

public sealed class ChangeRequestEvidence : EntityBase
{
    private ChangeRequestEvidence()
    {
    }

    public ChangeRequestEvidence(
        Guid changeRequestId,
        string fileName,
        string contentType,
        long sizeBytes,
        string contentHash,
        byte[] content)
    {
        ChangeRequestId = changeRequestId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        ContentHash = contentHash;
        Content = content;
    }

    public Guid ChangeRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
