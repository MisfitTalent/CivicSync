using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.Sync;

public sealed class SyncInboxEntry : EntityBase
{
    private SyncInboxEntry()
    {
    }

    public SyncInboxEntry(
        Guid departmentNodeId,
        Guid ledgerEntryId,
        Guid receivedFromNodeId,
        string citizenNationalIdNumber,
        string fieldChangesJson)
    {
        DepartmentNodeId = departmentNodeId;
        LedgerEntryId = ledgerEntryId;
        ReceivedFromNodeId = receivedFromNodeId;
        CitizenNationalIdNumber = citizenNationalIdNumber;
        FieldChangesJson = fieldChangesJson;
        Status = SyncStatus.Received;
    }

    public Guid DepartmentNodeId { get; set; }
    public Guid LedgerEntryId { get; set; }
    public Guid ReceivedFromNodeId { get; set; }
    public string CitizenNationalIdNumber { get; set; } = string.Empty;
    public string FieldChangesJson { get; set; } = "[]";
    public SyncStatus Status { get; set; }
    public DateTime? AppliedAtUtc { get; set; }

    public void MarkApplied()
    {
        Status = SyncStatus.Applied;
        AppliedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
