using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicSync.Application.Contracts.Ledger;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Ledger;
using CivicSync.Core.Domain.Sync;
using CivicSync.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Ledger;

public sealed class LedgerService : ILedgerService
{
    private const int DefaultMaximumBatchSize = 10;
    private const int AbsoluteMaximumBatchSize = 25;

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

    public async Task<ProcessApprovedChangeRequestsResponse> ProcessApprovedChangeRequestsAsync(
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        var boundedMaxItems = GetBoundedMaxItems(maxItems);
        var changeRequests = await _changeRequestRepository.GetQueryableAsync();
        var approvedChangeRequestIds = await changeRequests
            .Where(item => item.Status == ChangeRequestStatus.Approved)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => item.Id)
            .Take(boundedMaxItems)
            .ToListAsync(cancellationToken);

        var committedChanges = new List<CommitChangeRequestResponse>();
        var failures = new List<ChangeRequestProcessingFailureDto>();
        var conflictCount = 0;

        foreach (var changeRequestId in approvedChangeRequestIds)
        {
            try
            {
                committedChanges.Add(await CommitChangeRequestAsync(changeRequestId, cancellationToken));
            }
            catch (InvalidOperationException exception) when (IsCitizenVersionConflict(exception))
            {
                conflictCount++;
                failures.Add(new ChangeRequestProcessingFailureDto
                {
                    ChangeRequestId = changeRequestId,
                    Reason = exception.Message
                });
            }
        }

        return new ProcessApprovedChangeRequestsResponse
        {
            MaxItems = boundedMaxItems,
            ProcessedCount = approvedChangeRequestIds.Count,
            CommittedCount = committedChanges.Count,
            ConflictCount = conflictCount,
            FailureCount = failures.Count,
            CommittedChanges = committedChanges,
            Failures = failures
        };
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

        if (citizen.RecordVersion != changeRequest.ExpectedCitizenVersion)
        {
            changeRequest.MarkConflict();
            await _changeRequestRepository.UpdateAsync(changeRequest, autoSave: true, cancellationToken);

            throw new InvalidOperationException(
                $"Citizen record version conflict. Expected version {changeRequest.ExpectedCitizenVersion}, but current version is {citizen.RecordVersion}.");
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
            changeRequest.ExpectedCitizenVersion,
            CommittedCitizenVersion = citizen.RecordVersion,
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

        changeRequest.MarkCommitted(citizen.RecordVersion);

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

    private static int GetBoundedMaxItems(int maxItems)
    {
        if (maxItems <= 0)
        {
            return DefaultMaximumBatchSize;
        }

        return Math.Min(maxItems, AbsoluteMaximumBatchSize);
    }

    private static bool IsCitizenVersionConflict(InvalidOperationException exception)
    {
        return exception.Message.Contains("Citizen record version conflict", StringComparison.OrdinalIgnoreCase);
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
