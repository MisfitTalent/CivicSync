namespace CivicSync.Node.Api.Domain.Enums;

public enum ChangeRequestStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Committed = 5,
    Syncing = 6,
    Synced = 7,
    SyncFailed = 8,
    Conflict = 9
}
