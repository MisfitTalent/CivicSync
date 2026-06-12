using System.ComponentModel.DataAnnotations;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.ChangeRequests;

public sealed class RecordApprovalDecisionRequest
{
    [Required]
    public Guid ApprovingNodeId { get; set; }

    [Required]
    public Guid ApproverUserId { get; set; }

    [Required]
    public ApprovalDecision Decision { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }
}
