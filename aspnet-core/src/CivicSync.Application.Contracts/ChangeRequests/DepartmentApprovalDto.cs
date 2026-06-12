using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class DepartmentApprovalDto
{
    public Guid Id { get; set; }
    public Guid ApprovingNodeId { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverFullName { get; set; } = string.Empty;
    public string ApproverRole { get; set; } = string.Empty;
    public string ApproverDepartmentName { get; set; } = string.Empty;
    public ApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
}
