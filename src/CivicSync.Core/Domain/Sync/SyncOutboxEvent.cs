using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.Sync;

public sealed class SyncOutboxEvent : EntityBase
{
    private SyncOutboxEvent()
    {
    }

    public SyncOutboxEvent(Guid departmentNodeId, Guid ledgerEntryId)
    {
        DepartmentNodeId = departmentNodeId;
        LedgerEntryId = ledgerEntryId;
        Status = SyncStatus.Pending;
    }

    public Guid DepartmentNodeId { get; set; }
    public Guid LedgerEntryId { get; set; }
    public SyncStatus Status { get; set; }
    public int RetryCount { get; set; }

    public void MarkPublished()
    {
        Status = SyncStatus.Published;
        MarkUpdated();
    }

    public void MarkFailed()
    {
        RetryCount++;
        Status = SyncStatus.Failed;
        MarkUpdated();
    }
}
