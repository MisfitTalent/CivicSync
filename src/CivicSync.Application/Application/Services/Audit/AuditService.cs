using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Contracts.Audit;
using CivicSync.Node.Api.Domain.Ledger;
using CivicSync.Node.Api.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Node.Api.Application.Services.Audit;

public sealed class AuditService : IAuditService
{
    private readonly IRepository<LedgerEntry, Guid> _ledgerEntryRepository;
    private readonly IRepository<NodeSyncReceipt, Guid> _nodeSyncReceiptRepository;
    private readonly IRepository<SyncInboxEntry, Guid> _syncInboxEntryRepository;
    private readonly IRepository<SyncOutboxEvent, Guid> _syncOutboxEventRepository;
    private readonly NodeOptions _nodeOptions;
    private readonly HttpClient _httpClient;

    public AuditService(
        IRepository<LedgerEntry, Guid> ledgerEntryRepository,
        IRepository<NodeSyncReceipt, Guid> nodeSyncReceiptRepository,
        IRepository<SyncInboxEntry, Guid> syncInboxEntryRepository,
        IRepository<SyncOutboxEvent, Guid> syncOutboxEventRepository,
        IOptions<NodeOptions> nodeOptions,
        HttpClient httpClient)
    {
        _ledgerEntryRepository = ledgerEntryRepository;
        _nodeSyncReceiptRepository = nodeSyncReceiptRepository;
        _syncInboxEntryRepository = syncInboxEntryRepository;
        _syncOutboxEventRepository = syncOutboxEventRepository;
        _nodeOptions = nodeOptions.Value;
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<AuditLedgerEntryDto>> GetLedgerEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        var ledgerEntries = await _ledgerEntryRepository.GetQueryableAsync();
        return await ledgerEntries
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new AuditLedgerEntryDto
            {
                Id = item.Id,
                OriginatingNodeId = item.OriginatingNodeId,
                ChangeRequestId = item.ChangeRequestId,
                SequenceNumber = item.SequenceNumber,
                EventType = item.EventType,
                PayloadProofHash = item.PayloadProof.Hash,
                PreviousProofHash = item.PreviousProof.Hash,
                CurrentProofHash = item.CurrentProof.Hash,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditSyncOutboxEventDto>> GetOutboxEventsAsync(
        CancellationToken cancellationToken = default)
    {
        var outboxEvents = await _syncOutboxEventRepository.GetQueryableAsync();
        return await outboxEvents
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new AuditSyncOutboxEventDto
            {
                Id = item.Id,
                DepartmentNodeId = item.DepartmentNodeId,
                LedgerEntryId = item.LedgerEntryId,
                Status = item.Status,
                RetryCount = item.RetryCount,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditSyncInboxEntryDto>> GetInboxEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        var inboxEntries = await _syncInboxEntryRepository.GetQueryableAsync();
        return await inboxEntries
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new AuditSyncInboxEntryDto
            {
                Id = item.Id,
                DepartmentNodeId = item.DepartmentNodeId,
                LedgerEntryId = item.LedgerEntryId,
                ReceivedFromNodeId = item.ReceivedFromNodeId,
                CitizenNationalIdNumber = item.CitizenNationalIdNumber,
                FieldChangesJson = item.FieldChangesJson,
                Status = item.Status,
                AppliedAtUtc = item.AppliedAtUtc,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditNodeSyncReceiptDto>> GetSyncReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        var receipts = await _nodeSyncReceiptRepository.GetQueryableAsync();
        return await receipts
            .OrderByDescending(item => item.ReceivedAtUtc)
            .Select(item => new AuditNodeSyncReceiptDto
            {
                Id = item.Id,
                SyncOutboxEventId = item.SyncOutboxEventId,
                TargetNodeId = item.TargetNodeId,
                Result = item.Result,
                ReceivedAtUtc = item.ReceivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PeerHealthDto>> GetPeerHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var peerHealthTasks = _nodeOptions.Peers
            .Select(peer => GetSinglePeerHealthAsync(peer, cancellationToken))
            .ToList();

        return await Task.WhenAll(peerHealthTasks);
    }

    private async Task<PeerHealthDto> GetSinglePeerHealthAsync(
        PeerNodeOptions peer,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"{peer.ApiBaseUrl.TrimEnd('/')}/api/node",
                cancellationToken);

            return new PeerHealthDto
            {
                DepartmentCode = peer.DepartmentCode,
                ApiBaseUrl = peer.ApiBaseUrl,
                IsOnline = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode
                    ? "Peer responded successfully."
                    : $"Peer returned HTTP {(int)response.StatusCode}."
            };
        }
        catch (Exception exception)
        {
            return new PeerHealthDto
            {
                DepartmentCode = peer.DepartmentCode,
                ApiBaseUrl = peer.ApiBaseUrl,
                IsOnline = false,
                Message = exception.Message
            };
        }
    }
}
