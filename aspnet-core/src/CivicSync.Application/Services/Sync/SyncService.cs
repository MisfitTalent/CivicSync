using System.Net.Http.Json;
using System.Text.Json;
using CivicSync.Core.Configuration;
using CivicSync.Application.Contracts.Sync;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Ledger;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.Sync;
using CivicSync.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Sync;

public sealed class SyncService : ISyncService
{
    private readonly IRepository<ChangeRequest, Guid> _changeRequestRepository;
    private readonly IRepository<Citizen, Guid> _citizenRepository;
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly IRepository<LedgerEntry, Guid> _ledgerEntryRepository;
    private readonly IRepository<NodeSyncReceipt, Guid> _nodeSyncReceiptRepository;
    private readonly IRepository<SyncInboxEntry, Guid> _syncInboxEntryRepository;
    private readonly IRepository<SyncOutboxEvent, Guid> _syncOutboxEventRepository;
    private readonly NodeOptions _nodeOptions;
    private readonly HttpClient _httpClient;
    private readonly INodeSyncSignatureService _signatureService;

    public SyncService(
        IRepository<ChangeRequest, Guid> changeRequestRepository,
        IRepository<Citizen, Guid> citizenRepository,
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IRepository<LedgerEntry, Guid> ledgerEntryRepository,
        IRepository<NodeSyncReceipt, Guid> nodeSyncReceiptRepository,
        IRepository<SyncInboxEntry, Guid> syncInboxEntryRepository,
        IRepository<SyncOutboxEvent, Guid> syncOutboxEventRepository,
        IOptions<NodeOptions> nodeOptions,
        HttpClient httpClient,
        INodeSyncSignatureService signatureService)
    {
        _changeRequestRepository = changeRequestRepository;
        _citizenRepository = citizenRepository;
        _departmentNodeRepository = departmentNodeRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _nodeSyncReceiptRepository = nodeSyncReceiptRepository;
        _syncInboxEntryRepository = syncInboxEntryRepository;
        _syncOutboxEventRepository = syncOutboxEventRepository;
        _nodeOptions = nodeOptions.Value;
        _httpClient = httpClient;
        _signatureService = signatureService;
    }

    public async Task<SynchronizedLedgerEntryResponse> ReceiveLedgerEntryAsync(
        ReceiveLedgerEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var localNode = await GetLocalNodeAsync(cancellationToken);
        var ledgerEntries = await _ledgerEntryRepository.GetQueryableAsync();
        var ledgerEntry = await ledgerEntries.SingleOrDefaultAsync(item => item.Id == request.LedgerEntryId, cancellationToken);

        if (ledgerEntry is null)
        {
            ledgerEntry = new LedgerEntry(
                request.LedgerEntryId,
                request.OriginatingNodeId,
                request.ChangeRequestId,
                request.SequenceNumber,
                request.EventType,
                new RecordProof(request.PayloadProofHash),
                new RecordProof(request.PreviousProofHash),
                new RecordProof(request.CurrentProofHash));

            await _ledgerEntryRepository.InsertAsync(ledgerEntry, autoSave: false, cancellationToken);
        }

        var inboxEntries = await _syncInboxEntryRepository.GetQueryableAsync();
        var inboxEntry = await inboxEntries.SingleOrDefaultAsync(
            item => item.DepartmentNodeId == localNode.Id && item.LedgerEntryId == ledgerEntry.Id,
            cancellationToken);

        if (inboxEntry is null)
        {
            inboxEntry = new SyncInboxEntry(
                localNode.Id,
                ledgerEntry.Id,
                request.OriginatingNodeId,
                request.CitizenNationalIdNumber,
                JsonSerializer.Serialize(request.FieldChanges));
            await _syncInboxEntryRepository.InsertAsync(inboxEntry, autoSave: false, cancellationToken);
        }

        var citizen = await FindCitizenByNationalIdAsync(localNode.Id, request.CitizenNationalIdNumber, cancellationToken);
        var createdCitizen = false;

        if (citizen is null)
        {
            if (!CanCreateCitizenFromSyncPayload(request))
            {
                await _syncInboxEntryRepository.UpdateAsync(inboxEntry, autoSave: true, cancellationToken);

                return new SynchronizedLedgerEntryResponse
                {
                    LedgerEntryId = ledgerEntry.Id,
                    Result = SyncResult.Queued,
                    Message = "Ledger entry stored, but matching citizen does not exist on this node yet."
                };
            }

            citizen = CreateCitizenFromSyncPayload(localNode.Id, request);
            createdCitizen = true;
        }

        if (!createdCitizen)
        {
            foreach (var fieldChange in request.FieldChanges)
            {
                citizen.ApplySharedFieldChange(fieldChange.FieldName, fieldChange.NewValue);
            }
        }

        inboxEntry.MarkApplied();

        if (createdCitizen)
        {
            await _citizenRepository.InsertAsync(citizen, autoSave: false, cancellationToken);
        }
        else
        {
            await _citizenRepository.UpdateAsync(citizen, autoSave: false, cancellationToken);
        }

        await _syncInboxEntryRepository.UpdateAsync(inboxEntry, autoSave: true, cancellationToken);

        return new SynchronizedLedgerEntryResponse
        {
            LedgerEntryId = ledgerEntry.Id,
            Result = SyncResult.Applied,
            Message = createdCitizen
                ? "Ledger entry created the missing citizen replica on this node."
                : "Ledger entry was applied to the local citizen record."
        };
    }

    public async Task<PublishOutboxResponse> PublishPendingOutboxEventsAsync(CancellationToken cancellationToken = default)
    {
        var localNode = await GetLocalNodeWithPeersAsync(cancellationToken);
        var outboxEvents = await _syncOutboxEventRepository.GetQueryableAsync();
        var pendingOutboxEvents = await outboxEvents
            .Where(item => item.DepartmentNodeId == localNode.Id && item.Status != SyncStatus.Published)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = new PublishOutboxResponse();
        var peerResults = new List<PeerSyncResultDto>();

        foreach (var outboxEvent in pendingOutboxEvents)
        {
            if (outboxEvent.Status == SyncStatus.Failed &&
                outboxEvent.RetryCount >= _nodeOptions.MaxSyncPublishAttempts)
            {
                response.SkippedOutboxEvents++;
                continue;
            }

            var syncRequest = await BuildSyncRequestAsync(outboxEvent.LedgerEntryId, cancellationToken);
            var eventDeliveredToAllPeers = true;

            foreach (var peer in _nodeOptions.Peers)
            {
                var peerResult = await PublishToPeerAsync(outboxEvent, peer, syncRequest, localNode, cancellationToken);
                peerResults.Add(peerResult);

                if (peerResult.Result is SyncResult.Applied or SyncResult.Queued)
                {
                    response.SuccessfulPeerDeliveries++;
                    continue;
                }

                response.FailedPeerDeliveries++;
                eventDeliveredToAllPeers = false;
            }

            if (eventDeliveredToAllPeers)
            {
                outboxEvent.MarkPublished();
            }
            else
            {
                outboxEvent.MarkFailed();
            }

            await _syncOutboxEventRepository.UpdateAsync(outboxEvent, autoSave: false, cancellationToken);
            response.ProcessedOutboxEvents++;
        }

        if (pendingOutboxEvents.Count > 0)
        {
            await _syncOutboxEventRepository.UpdateAsync(pendingOutboxEvents[^1], autoSave: true, cancellationToken);
        }

        response.PeerResults = peerResults;
        return response;
    }

    public async Task<ApplyPendingInboxResponse> ApplyPendingInboxEntriesAsync(CancellationToken cancellationToken = default)
    {
        var localNode = await GetLocalNodeAsync(cancellationToken);
        var inboxEntries = await _syncInboxEntryRepository.GetQueryableAsync();
        var pendingInboxEntries = await inboxEntries
            .Where(item => item.DepartmentNodeId == localNode.Id && item.Status != SyncStatus.Applied)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = new ApplyPendingInboxResponse();
        var results = new List<SynchronizedLedgerEntryResponse>();

        foreach (var inboxEntry in pendingInboxEntries)
        {
            response.ProcessedInboxEntries++;

            if (string.IsNullOrWhiteSpace(inboxEntry.CitizenNationalIdNumber) ||
                string.IsNullOrWhiteSpace(inboxEntry.FieldChangesJson))
            {
                response.StillQueuedInboxEntries++;
                results.Add(new SynchronizedLedgerEntryResponse
                {
                    LedgerEntryId = inboxEntry.LedgerEntryId,
                    Result = SyncResult.Queued,
                    Message = "Inbox entry was queued before sync payload storage existed and cannot be auto-applied."
                });
                continue;
            }

            var citizen = await FindCitizenByNationalIdAsync(localNode.Id, inboxEntry.CitizenNationalIdNumber, cancellationToken);

            if (citizen is null)
            {
                response.StillQueuedInboxEntries++;
                results.Add(new SynchronizedLedgerEntryResponse
                {
                    LedgerEntryId = inboxEntry.LedgerEntryId,
                    Result = SyncResult.Queued,
                    Message = "Matching citizen does not exist on this node yet."
                });
                continue;
            }

            var fieldChanges = JsonSerializer.Deserialize<List<SyncedFieldChangeDto>>(inboxEntry.FieldChangesJson) ?? [];
            foreach (var fieldChange in fieldChanges)
            {
                citizen.ApplySharedFieldChange(fieldChange.FieldName, fieldChange.NewValue);
            }

            inboxEntry.MarkApplied();
            await _citizenRepository.UpdateAsync(citizen, autoSave: false, cancellationToken);
            await _syncInboxEntryRepository.UpdateAsync(inboxEntry, autoSave: false, cancellationToken);
            response.AppliedInboxEntries++;
            results.Add(new SynchronizedLedgerEntryResponse
            {
                LedgerEntryId = inboxEntry.LedgerEntryId,
                Result = SyncResult.Applied,
                Message = "Queued inbox entry was applied to the local citizen record."
            });
        }

        if (pendingInboxEntries.Count > 0)
        {
            await _syncInboxEntryRepository.UpdateAsync(pendingInboxEntries[^1], autoSave: true, cancellationToken);
        }

        response.Results = results;
        return response;
    }

    private async Task<ReceiveLedgerEntryRequest> BuildSyncRequestAsync(Guid ledgerEntryId, CancellationToken cancellationToken)
    {
        var ledgerEntries = await _ledgerEntryRepository.GetQueryableAsync();
        var ledgerEntry = await ledgerEntries.SingleAsync(item => item.Id == ledgerEntryId, cancellationToken);
        var changeRequests = await _changeRequestRepository.WithDetailsAsync(item => item.FieldChanges);
        var changeRequest = await changeRequests.SingleAsync(item => item.Id == ledgerEntry.ChangeRequestId, cancellationToken);
        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizen = await citizens.SingleAsync(item => item.Id == changeRequest.CitizenId, cancellationToken);

        return new ReceiveLedgerEntryRequest
        {
            LedgerEntryId = ledgerEntry.Id,
            OriginatingNodeId = ledgerEntry.OriginatingNodeId,
            ChangeRequestId = ledgerEntry.ChangeRequestId,
            SequenceNumber = ledgerEntry.SequenceNumber,
            EventType = ledgerEntry.EventType,
            PayloadProofHash = ledgerEntry.PayloadProof.Hash,
            PreviousProofHash = ledgerEntry.PreviousProof.Hash,
            CurrentProofHash = ledgerEntry.CurrentProof.Hash,
            CitizenNationalIdNumber = citizen.NationalIdNumber,
            CitizenFirstName = citizen.FullName.FirstName,
            CitizenLastName = citizen.FullName.LastName,
            CitizenEmailAddress = citizen.ContactDetails.EmailAddress,
            CitizenPhoneNumber = citizen.ContactDetails.PhoneNumber,
            CitizenDateOfBirth = citizen.DateOfBirth,
            CitizenPassportNumber = citizen.PassportNumber,
            CitizenBiometricReference = citizen.BiometricReference,
            CitizenRelationshipStatus = citizen.RelationshipStatus,
            CitizenTaxNumber = citizen.TaxNumber,
            CitizenEmploymentHistory = citizen.EmploymentHistory,
            CitizenIncomeAndInvestmentProfile = citizen.IncomeAndInvestmentProfile,
            CitizenBankingAndAssets = citizen.BankingAndAssets,
            CitizenResidentialAddress = citizen.ResidentialAddress,
            CitizenRatesAccount = citizen.RatesAccount,
            CitizenMunicipalServiceStatus = citizen.MunicipalServiceStatus,
            FieldChanges = changeRequest.FieldChanges
                .Select(item => new SyncedFieldChangeDto
                {
                    FieldName = item.FieldName,
                    NewValue = item.NewValue
                })
                .ToList()
        };
    }

    private async Task<PeerSyncResultDto> PublishToPeerAsync(
        SyncOutboxEvent outboxEvent,
        PeerNodeOptions peer,
        ReceiveLedgerEntryRequest syncRequest,
        DepartmentNode localNode,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{peer.ApiBaseUrl.TrimEnd('/')}/api/sync/ledger-entries";
            var timestampUtc = DateTimeOffset.UtcNow.ToString("O");
            var signature = _signatureService.CreateSignature(
                syncRequest,
                _nodeOptions.DepartmentCode,
                timestampUtc,
                peer.SharedSecret);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(syncRequest)
            };
            httpRequest.Headers.Add("X-CivicSync-Node", _nodeOptions.DepartmentCode.ToString());
            httpRequest.Headers.Add("X-CivicSync-Timestamp", timestampUtc);
            httpRequest.Headers.Add("X-CivicSync-Signature", signature);

            var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return CreatePeerResult(outboxEvent, peer, SyncResult.Failed, $"Peer returned HTTP {(int)httpResponse.StatusCode}.");
            }

            var syncResponse = await httpResponse.Content.ReadFromJsonAsync<SynchronizedLedgerEntryResponse>(
                cancellationToken: cancellationToken);

            var result = syncResponse?.Result ?? SyncResult.Failed;
            await RecordReceiptAsync(outboxEvent, localNode, peer, result, cancellationToken);

            return CreatePeerResult(
                outboxEvent,
                peer,
                result,
                syncResponse?.Message ?? "Peer response did not include a message.");
        }
        catch (Exception exception)
        {
            return CreatePeerResult(outboxEvent, peer, SyncResult.Failed, exception.Message);
        }
    }

    private async Task RecordReceiptAsync(
        SyncOutboxEvent outboxEvent,
        DepartmentNode localNode,
        PeerNodeOptions peer,
        SyncResult result,
        CancellationToken cancellationToken)
    {
        var targetPeer = localNode.KnownPeers.SingleOrDefault(item => item.PeerDepartmentCode == peer.DepartmentCode);

        if (targetPeer is null)
        {
            return;
        }

        var receipts = await _nodeSyncReceiptRepository.GetQueryableAsync();
        var existingReceipt = await receipts.SingleOrDefaultAsync(
            item => item.SyncOutboxEventId == outboxEvent.Id && item.TargetNodeId == targetPeer.Id,
            cancellationToken);

        if (existingReceipt is null)
        {
            await _nodeSyncReceiptRepository.InsertAsync(new NodeSyncReceipt(outboxEvent.Id, targetPeer.Id, result), autoSave: false, cancellationToken);
            return;
        }

        existingReceipt.Result = result;
        existingReceipt.ReceivedAtUtc = DateTime.UtcNow;
        await _nodeSyncReceiptRepository.UpdateAsync(existingReceipt, autoSave: false, cancellationToken);
    }

    private static bool CanCreateCitizenFromSyncPayload(ReceiveLedgerEntryRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.CitizenNationalIdNumber) &&
            !string.IsNullOrWhiteSpace(request.CitizenFirstName) &&
            !string.IsNullOrWhiteSpace(request.CitizenLastName) &&
            !string.IsNullOrWhiteSpace(request.CitizenEmailAddress) &&
            !string.IsNullOrWhiteSpace(request.CitizenPhoneNumber);
    }

    private static Citizen CreateCitizenFromSyncPayload(Guid departmentNodeId, ReceiveLedgerEntryRequest request)
    {
        return new Citizen(
            departmentNodeId,
            request.CitizenNationalIdNumber.Trim(),
            new PersonName(request.CitizenFirstName.Trim(), request.CitizenLastName.Trim()),
            new ContactDetails(request.CitizenEmailAddress.Trim(), request.CitizenPhoneNumber.Trim()))
        {
            DateOfBirth = NormalizeOptionalSyncValue(request.CitizenDateOfBirth),
            PassportNumber = NormalizeOptionalSyncValue(request.CitizenPassportNumber),
            BiometricReference = NormalizeOptionalSyncValue(request.CitizenBiometricReference),
            RelationshipStatus = NormalizeOptionalSyncValue(request.CitizenRelationshipStatus),
            TaxNumber = NormalizeOptionalSyncValue(request.CitizenTaxNumber),
            EmploymentHistory = NormalizeOptionalSyncValue(request.CitizenEmploymentHistory),
            IncomeAndInvestmentProfile = NormalizeOptionalSyncValue(request.CitizenIncomeAndInvestmentProfile),
            BankingAndAssets = NormalizeOptionalSyncValue(request.CitizenBankingAndAssets),
            ResidentialAddress = NormalizeOptionalSyncValue(request.CitizenResidentialAddress),
            RatesAccount = NormalizeOptionalSyncValue(request.CitizenRatesAccount),
            MunicipalServiceStatus = NormalizeOptionalSyncValue(request.CitizenMunicipalServiceStatus)
        };
    }

    private static string NormalizeOptionalSyncValue(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private async Task<DepartmentNode> GetLocalNodeAsync(CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        return await departmentNodes.SingleAsync(item => item.DepartmentCode == _nodeOptions.DepartmentCode, cancellationToken);
    }

    private async Task<DepartmentNode> GetLocalNodeWithPeersAsync(CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.WithDetailsAsync(item => item.KnownPeers);
        return await departmentNodes.SingleAsync(item => item.DepartmentCode == _nodeOptions.DepartmentCode, cancellationToken);
    }

    private async Task<Citizen?> FindCitizenByNationalIdAsync(
        Guid departmentNodeId,
        string nationalIdNumber,
        CancellationToken cancellationToken)
    {
        var citizens = await _citizenRepository.GetQueryableAsync();
        return await citizens.SingleOrDefaultAsync(
            item => item.DepartmentNodeId == departmentNodeId && item.NationalIdNumber == nationalIdNumber,
            cancellationToken);
    }

    private static PeerSyncResultDto CreatePeerResult(
        SyncOutboxEvent outboxEvent,
        PeerNodeOptions peer,
        SyncResult result,
        string message)
    {
        return new PeerSyncResultDto
        {
            SyncOutboxEventId = outboxEvent.Id,
            DepartmentCode = peer.DepartmentCode,
            ApiBaseUrl = peer.ApiBaseUrl,
            Result = result,
            RetryCount = outboxEvent.RetryCount,
            Message = message
        };
    }
}

