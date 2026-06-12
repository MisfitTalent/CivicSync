using CivicSync.Application.Services.Audit;
using CivicSync.Application.Contracts.Audit;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Host.Controllers;

[ApiController]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyCollection<AuditLedgerEntryDto>>> GetLedgerEntriesAsync(
        CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetLedgerEntriesAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("outbox")]
    public async Task<ActionResult<IReadOnlyCollection<AuditSyncOutboxEventDto>>> GetOutboxEventsAsync(
        CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetOutboxEventsAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<IReadOnlyCollection<AuditSyncInboxEntryDto>>> GetInboxEntriesAsync(
        CancellationToken cancellationToken)
    {
        var entries = await _auditService.GetInboxEntriesAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpGet("sync-receipts")]
    public async Task<ActionResult<IReadOnlyCollection<AuditNodeSyncReceiptDto>>> GetSyncReceiptsAsync(
        CancellationToken cancellationToken)
    {
        var receipts = await _auditService.GetSyncReceiptsAsync(cancellationToken);

        return Ok(receipts);
    }

    [HttpGet("peer-health")]
    public async Task<ActionResult<IReadOnlyCollection<PeerHealthDto>>> GetPeerHealthAsync(
        CancellationToken cancellationToken)
    {
        var peers = await _auditService.GetPeerHealthAsync(cancellationToken);

        return Ok(peers);
    }
}
