using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Sync;

public sealed class PeerSyncResultDto
{
    public Guid SyncOutboxEventId { get; set; }
    public DepartmentCode DepartmentCode { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public SyncResult Result { get; set; }
    public int RetryCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
