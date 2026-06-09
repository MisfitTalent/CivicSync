namespace CivicSync.Core.Domain.Enums;

public enum SyncResult
{
    Pending = 1,
    Applied = 2,
    Queued = 3,
    Rejected = 4,
    Failed = 5
}
