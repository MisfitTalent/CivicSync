using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.ChangeRequests;

public sealed class ChangeRequest : EntityBase
{
    private readonly List<FieldChange> _fieldChanges = [];
    private readonly List<DepartmentApproval> _approvals = [];

    private ChangeRequest()
    {
    }

    public ChangeRequest(Guid requestedAtNodeId, Guid citizenId, string reason, long expectedCitizenVersion)
    {
        RequestedAtNodeId = requestedAtNodeId;
        CitizenId = citizenId;
        Reason = reason;
        ExpectedCitizenVersion = expectedCitizenVersion;
        Status = ChangeRequestStatus.Draft;
    }

    public Guid RequestedAtNodeId { get; set; }
    public Guid CitizenId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long ExpectedCitizenVersion { get; set; }
    public long? CommittedCitizenVersion { get; set; }
    public ChangeRequestStatus Status { get; set; }
    public IReadOnlyCollection<FieldChange> FieldChanges => _fieldChanges.AsReadOnly();
    public IReadOnlyCollection<DepartmentApproval> Approvals => _approvals.AsReadOnly();

    public void AddFieldChange(string fieldName, string oldValue, string newValue)
    {
        if (Status != ChangeRequestStatus.Draft)
        {
            throw new InvalidOperationException("Field changes can only be added while the request is still a draft.");
        }

        _fieldChanges.Add(new FieldChange(Id, fieldName, oldValue, newValue));
        MarkUpdated();
    }

    public DepartmentApproval RequestApprovalFrom(
        Guid approvingNodeId,
        Guid approverUserId,
        string approverFullName,
        string approverRole,
        string approverDepartmentName)
    {
        var existingApproval = _approvals.SingleOrDefault(approval => approval.ApprovingNodeId == approvingNodeId);

        if (existingApproval is not null)
        {
            return existingApproval;
        }

        var approval = new DepartmentApproval(
            Id,
            approvingNodeId,
            approverUserId,
            approverFullName,
            approverRole,
            approverDepartmentName);
        _approvals.Add(approval);
        Status = ChangeRequestStatus.PendingApproval;
        MarkUpdated();

        return approval;
    }

    public void RecordDecision(Guid approvingNodeId, ApprovalDecision decision, string? comment)
    {
        var approval = _approvals.SingleOrDefault(item => item.ApprovingNodeId == approvingNodeId)
            ?? throw new InvalidOperationException("The selected node is not required to approve this change request.");

        approval.RecordDecision(decision, comment);
        Status = _approvals.Any(item => item.Decision == ApprovalDecision.Rejected)
            ? ChangeRequestStatus.Rejected
            : _approvals.All(item => item.Decision == ApprovalDecision.Approved)
                ? ChangeRequestStatus.Approved
                : ChangeRequestStatus.PendingApproval;

        MarkUpdated();
    }

    public void MarkConflict()
    {
        if (Status != ChangeRequestStatus.Approved)
        {
            throw new InvalidOperationException("Only approved change requests can be marked as conflicted.");
        }

        Status = ChangeRequestStatus.Conflict;
        MarkUpdated();
    }

    public void MarkCommitted(long committedCitizenVersion)
    {
        if (Status != ChangeRequestStatus.Approved)
        {
            throw new InvalidOperationException("Only approved change requests can be committed.");
        }

        CommittedCitizenVersion = committedCitizenVersion;
        Status = ChangeRequestStatus.Committed;
        MarkUpdated();
    }
}

