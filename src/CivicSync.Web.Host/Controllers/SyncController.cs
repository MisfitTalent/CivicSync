using CivicSync.Core.Configuration;
using CivicSync.Application.Services.Sync;
using CivicSync.Application.Contracts.Sync;
using CivicSync.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Controllers;

[ApiController]
[Route("api/sync")]
public sealed class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly INodeSyncSignatureService _signatureService;
    private readonly NodeOptions _nodeOptions;

    public SyncController(
        ISyncService syncService,
        INodeSyncSignatureService signatureService,
        IOptions<NodeOptions> nodeOptions)
    {
        _syncService = syncService;
        _signatureService = signatureService;
        _nodeOptions = nodeOptions.Value;
    }

    [HttpPost("ledger-entries")]
    public async Task<ActionResult<SynchronizedLedgerEntryResponse>> ReceiveLedgerEntryAsync(
        ReceiveLedgerEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryValidateNodeSignature(request))
        {
            return Unauthorized(new SynchronizedLedgerEntryResponse
            {
                LedgerEntryId = request.LedgerEntryId,
                Result = SyncResult.Rejected,
                Message = "Node sync signature is missing or invalid."
            });
        }

        var response = await _syncService.ReceiveLedgerEntryAsync(request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("outbox/publish-pending")]
    public async Task<ActionResult<PublishOutboxResponse>> PublishPendingOutboxEventsAsync(
        CancellationToken cancellationToken)
    {
        var response = await _syncService.PublishPendingOutboxEventsAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPost("inbox/apply-pending")]
    public async Task<ActionResult<ApplyPendingInboxResponse>> ApplyPendingInboxEntriesAsync(
        CancellationToken cancellationToken)
    {
        var response = await _syncService.ApplyPendingInboxEntriesAsync(cancellationToken);

        return Ok(response);
    }

    private bool TryValidateNodeSignature(ReceiveLedgerEntryRequest request)
    {
        var sendingNode = Request.Headers["X-CivicSync-Node"].ToString();
        var timestampUtc = Request.Headers["X-CivicSync-Timestamp"].ToString();
        var signature = Request.Headers["X-CivicSync-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(sendingNode) ||
            string.IsNullOrWhiteSpace(timestampUtc) ||
            string.IsNullOrWhiteSpace(signature) ||
            !Enum.TryParse<DepartmentCode>(sendingNode, out var sendingDepartmentCode))
        {
            return false;
        }

        return _signatureService.IsValidSignature(
            request,
            sendingDepartmentCode,
            timestampUtc,
            signature,
            _nodeOptions.SharedSecret);
    }
}
