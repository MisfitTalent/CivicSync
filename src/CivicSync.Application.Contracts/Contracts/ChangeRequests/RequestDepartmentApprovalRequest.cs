using System.ComponentModel.DataAnnotations;

namespace CivicSync.Node.Api.Contracts.ChangeRequests;

public sealed class RequestDepartmentApprovalRequest
{
    [Required]
    public Guid ApprovingNodeId { get; set; }

    [Required]
    public Guid ApproverUserId { get; set; }
}
