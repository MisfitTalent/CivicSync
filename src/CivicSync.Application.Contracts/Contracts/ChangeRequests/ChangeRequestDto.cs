using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.ChangeRequests;

public sealed class ChangeRequestDto
{
    public Guid Id { get; set; }
    public Guid RequestedAtNodeId { get; set; }
    public Guid CitizenId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long ExpectedCitizenVersion { get; set; }
    public long? CommittedCitizenVersion { get; set; }
    public ChangeRequestStatus Status { get; set; }
    public IReadOnlyCollection<FieldChangeDto> FieldChanges { get; set; } = [];
    public IReadOnlyCollection<DepartmentApprovalDto> Approvals { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
}
