using CivicSync.Node.Api.Domain.Common;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Domain.ChangeRequests;

public sealed class DepartmentApproval : EntityBase
{
    private DepartmentApproval()
    {
    }

    public DepartmentApproval(
        Guid changeRequestId,
        Guid approvingNodeId,
        Guid approverUserId,
        string approverFullName,
        string approverRole,
        string approverDepartmentName)
    {
        ChangeRequestId = changeRequestId;
        ApprovingNodeId = approvingNodeId;
        ApproverUserId = approverUserId;
        ApproverFullName = approverFullName;
        ApproverRole = approverRole;
        ApproverDepartmentName = approverDepartmentName;
        Decision = ApprovalDecision.Pending;
    }

    public Guid ChangeRequestId { get; set; }
    public Guid ApprovingNodeId { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverFullName { get; set; } = string.Empty;
    public string ApproverRole { get; set; } = string.Empty;
    public string ApproverDepartmentName { get; set; } = string.Empty;
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public void RecordDecision(ApprovalDecision decision, string? comment)
    {
        if (decision == ApprovalDecision.Pending)
        {
            throw new InvalidOperationException("A final approval decision cannot be pending.");
        }

        Decision = decision;
        Comment = comment;
        DecidedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }
}
