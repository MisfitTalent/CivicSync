using CivicSync.Application.Services.ChangeRequests;
using CivicSync.Application.Services.Ledger;
using CivicSync.Application.Services.Sync;
using CivicSync.Application.Contracts.ChangeRequests;
using CivicSync.Application.Contracts.Ledger;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Host.Controllers;

[ApiController]
[Route("api/change-requests")]
public sealed class ChangeRequestsController : ControllerBase
{
    private readonly IChangeRequestService _changeRequestService;
    private readonly ILedgerService _ledgerService;
    private readonly ISyncService _syncService;
    private readonly ILogger<ChangeRequestsController> _logger;

    public ChangeRequestsController(
        IChangeRequestService changeRequestService,
        ILedgerService ledgerService,
        ISyncService syncService,
        ILogger<ChangeRequestsController> logger)
    {
        _changeRequestService = changeRequestService;
        _ledgerService = ledgerService;
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChangeRequestDto>> SubmitAsync(
        SubmitChangeRequest request,
        CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestService.SubmitAsync(request, cancellationToken);

        return Created($"/api/change-requests/{changeRequest.Id}", changeRequest);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ChangeRequestDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var changeRequests = await _changeRequestService.GetAllAsync(cancellationToken);

        return Ok(changeRequests);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChangeRequestDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestService.GetByIdAsync(id, cancellationToken);

        return changeRequest is null ? NotFound() : Ok(changeRequest);
    }

    [HttpGet("{id:guid}/evidence/{evidenceFileId:guid}")]
    public async Task<IActionResult> DownloadEvidenceFileAsync(
        Guid id,
        Guid evidenceFileId,
        CancellationToken cancellationToken)
    {
        var evidenceFile = await _changeRequestService.GetEvidenceFileAsync(id, evidenceFileId, cancellationToken);

        return evidenceFile is null
            ? NotFound()
            : File(evidenceFile.Content, evidenceFile.ContentType, evidenceFile.FileName);
    }

    [HttpPost("{id:guid}/approvals")]
    public async Task<ActionResult<ChangeRequestDto>> RequestApprovalAsync(
        Guid id,
        RequestDepartmentApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestService.RequestApprovalAsync(id, request, cancellationToken);

        return Ok(changeRequest);
    }

    [HttpPost("{id:guid}/decisions")]
    public async Task<ActionResult<ChangeRequestDto>> RecordDecisionAsync(
        Guid id,
        RecordApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var changeRequest = await _changeRequestService.RecordDecisionAsync(id, request, cancellationToken);

        return Ok(changeRequest);
    }

    [HttpPost("{id:guid}/commit")]
    public async Task<ActionResult<CommitChangeRequestResponse>> CommitAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _ledgerService.CommitChangeRequestAsync(id, cancellationToken);
        try
        {
            await _syncService.PublishPendingOutboxEventsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Change request {ChangeRequestId} was committed but immediate peer sync failed.", id);
        }

        return Ok(response);
    }

    [HttpPost("process-approved")]
    public async Task<ActionResult<ProcessApprovedChangeRequestsResponse>> ProcessApprovedAsync(
        ProcessApprovedChangeRequestsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _ledgerService.ProcessApprovedChangeRequestsAsync(request.MaxItems, cancellationToken);

        return Ok(response);
    }
}
