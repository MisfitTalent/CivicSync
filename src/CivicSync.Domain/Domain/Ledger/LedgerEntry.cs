using CivicSync.Node.Api.Domain.Common;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.ValueObjects;

namespace CivicSync.Node.Api.Domain.Ledger;

public sealed class LedgerEntry : EntityBase
{
    private LedgerEntry()
    {
    }

    public LedgerEntry(
        Guid originatingNodeId,
        Guid changeRequestId,
        long sequenceNumber,
        LedgerEventType eventType,
        RecordProof payloadProof,
        RecordProof previousProof,
        RecordProof currentProof)
    {
        OriginatingNodeId = originatingNodeId;
        ChangeRequestId = changeRequestId;
        SequenceNumber = sequenceNumber;
        EventType = eventType;
        PayloadProof = payloadProof;
        PreviousProof = previousProof;
        CurrentProof = currentProof;
    }


    public LedgerEntry(
        Guid id,
        Guid originatingNodeId,
        Guid changeRequestId,
        long sequenceNumber,
        LedgerEventType eventType,
        RecordProof payloadProof,
        RecordProof previousProof,
        RecordProof currentProof)
        : this(originatingNodeId, changeRequestId, sequenceNumber, eventType, payloadProof, previousProof, currentProof)
    {
        Id = id;
    }
    public Guid OriginatingNodeId { get; set; }
    public Guid ChangeRequestId { get; set; }
    public long SequenceNumber { get; set; }
    public LedgerEventType EventType { get; set; }
    public RecordProof PayloadProof { get; set; } = new(string.Empty);
    public RecordProof PreviousProof { get; set; } = new(string.Empty);
    public RecordProof CurrentProof { get; set; } = new(string.Empty);
}

