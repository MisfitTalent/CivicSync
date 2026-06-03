using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Sync;

public sealed class PeerSyncResultDto
{
    public Guid SyncOutboxEventId { get; set; }
    public DepartmentCode DepartmentCode { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public SyncResult Result { get; set; }
    public int RetryCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
