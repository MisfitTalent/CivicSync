using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicSync.Node.Api.Contracts.Ledger;
using CivicSync.Node.Api.Domain.ChangeRequests;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Ledger;
using CivicSync.Node.Api.Domain.Sync;
using CivicSync.Node.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Node.Api.Application.Services.Ledger;

public sealed class LedgerService : ILedgerService
{
    private readonly IRepository<ChangeRequest, Guid> _changeRequestRepository;
    private readonly IRepository<Citizen, Guid> _citizenRepository;
    private readonly IRepository<LedgerEntry, Guid> _ledgerEntryRepository;
    private readonly IRepository<SyncOutboxEvent, Guid> _syncOutboxEventRepository;

    public LedgerService(
        IRepository<ChangeRequest, Guid> changeRequestRepository,
        IRepository<Citizen, Guid> citizenRepository,
        IRepository<LedgerEntry, Guid> ledgerEntryRepository,
        IRepository<SyncOutboxEvent, Guid> syncOutboxEventRepository)
    {
        _changeRequestRepository = changeRequestRepository;
        _citizenRepository = citizenRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _syncOutboxEventRepository = syncOutboxEventRepository;
    }

    public async Task<CommitChangeRequestResponse> CommitChangeRequestAsync(
        Guid changeRequestId,
        CancellationToken cancellationToken = default)
    {
        var changeRequests = await _changeRequestRepository.WithDetailsAsync(item => item.FieldChanges, item => item.Approvals);
        var changeRequest = await changeRequests.SingleOrDefaultAsync(item => item.Id == changeRequestId, cancellationToken);

        if (changeRequest is null)
        {
            throw new InvalidOperationException("Change request was not found.");
        }

        if (changeRequest.Status != ChangeRequestStatus.Approved)
        {
            throw new InvalidOperationException("Only approved change requests can be committed.");
        }

        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizen = await citizens.SingleOrDefaultAsync(item => item.Id == changeRequest.CitizenId, cancellationToken);

        if (citizen is null)
        {
            throw new InvalidOperationException("Citizen was not found.");
        }

        var ledgerEntries = await _ledgerEntryRepository.GetQueryableAsync();
        var latestLedgerEntry = await ledgerEntries
            .Where(item => item.OriginatingNodeId == changeRequest.RequestedAtNodeId)
            .OrderByDescending(item => item.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequenceNumber = (latestLedgerEntry?.SequenceNumber ?? 0) + 1;
        var previousHash = latestLedgerEntry?.CurrentProof.Hash ?? "GENESIS";

        foreach (var fieldChange in changeRequest.FieldChanges)
        {
            citizen.ApplySharedFieldChange(fieldChange.FieldName, fieldChange.NewValue);
        }

        var payload = new
        {
            changeRequest.Id,
            changeRequest.CitizenId,
            changeRequest.RequestedAtNodeId,
            FieldChanges = changeRequest.FieldChanges.Select(item => new
            {
                item.FieldName,
                item.OldValue,
                item.NewValue
            })
        };

        var payloadHash = ComputeHash(JsonSerializer.Serialize(payload));
        var currentHash = ComputeHash($"{previousHash}|{payloadHash}|{nextSequenceNumber}");

        var ledgerEntry = new LedgerEntry(
            changeRequest.RequestedAtNodeId,
            changeRequest.Id,
            nextSequenceNumber,
            LedgerEventType.ChangeCommitted,
            new RecordProof(payloadHash),
            new RecordProof(previousHash),
            new RecordProof(currentHash));

        changeRequest.MarkCommitted();

        var outboxEvent = new SyncOutboxEvent(changeRequest.RequestedAtNodeId, ledgerEntry.Id);

        await _citizenRepository.UpdateAsync(citizen, autoSave: false, cancellationToken);
        await _changeRequestRepository.UpdateAsync(changeRequest, autoSave: false, cancellationToken);
        await _ledgerEntryRepository.InsertAsync(ledgerEntry, autoSave: false, cancellationToken);
        await _syncOutboxEventRepository.InsertAsync(outboxEvent, autoSave: true, cancellationToken);

        return new CommitChangeRequestResponse
        {
            ChangeRequestId = changeRequest.Id,
            Status = changeRequest.Status.ToString(),
            LedgerEntry = MapToDto(ledgerEntry)
        };
    }

    private static LedgerEntryDto MapToDto(LedgerEntry ledgerEntry)
    {
        return new LedgerEntryDto
        {
            Id = ledgerEntry.Id,
            OriginatingNodeId = ledgerEntry.OriginatingNodeId,
            ChangeRequestId = ledgerEntry.ChangeRequestId,
            SequenceNumber = ledgerEntry.SequenceNumber,
            EventType = ledgerEntry.EventType,
            PayloadProofHash = ledgerEntry.PayloadProof.Hash,
            PreviousProofHash = ledgerEntry.PreviousProof.Hash,
            CurrentProofHash = ledgerEntry.CurrentProof.Hash,
            CreatedAtUtc = ledgerEntry.CreatedAtUtc
        };
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
