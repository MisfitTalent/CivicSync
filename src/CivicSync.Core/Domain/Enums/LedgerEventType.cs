namespace CivicSync.Core.Domain.Enums;

public enum LedgerEventType
{
    ChangeSubmitted = 1,
    ChangeApproved = 2,
    ChangeRejected = 3,
    ChangeCommitted = 4,
    ReplicaSynced = 5,
    SyncFailed = 6
}
