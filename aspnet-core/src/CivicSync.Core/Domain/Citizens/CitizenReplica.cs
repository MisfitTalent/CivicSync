using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.Citizens;

public sealed class CitizenReplica : EntityBase
{
    private CitizenReplica()
    {
    }

    public CitizenReplica(Guid departmentNodeId, Guid citizenId, string sharedDataJson)
    {
        DepartmentNodeId = departmentNodeId;
        CitizenId = citizenId;
        SharedDataJson = sharedDataJson;
        SyncStatus = SyncStatus.Applied;
    }

    public Guid DepartmentNodeId { get; set; }
    public Guid CitizenId { get; set; }
    public string SharedDataJson { get; set; } = "{}";
    public long Version { get; set; }
    public long LastLedgerSequenceApplied { get; set; }
    public SyncStatus SyncStatus { get; set; }

    public void ApplyLedgerEntry(long ledgerSequence, string sharedDataJson)
    {
        if (ledgerSequence <= LastLedgerSequenceApplied)
        {
            return;
        }

        SharedDataJson = sharedDataJson;
        LastLedgerSequenceApplied = ledgerSequence;
        Version++;
        SyncStatus = SyncStatus.Applied;
        MarkUpdated();
    }
}
