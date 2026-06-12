using CivicSync.Application.Contracts.ChangeRequests;

namespace CivicSync.Application.Services.ChangeRequests;

public interface IChangeRequestService
{
    Task<ChangeRequestDto> SubmitAsync(SubmitChangeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ChangeRequestDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ChangeRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EvidenceFileContentDto?> GetEvidenceFileAsync(Guid id, Guid evidenceFileId, CancellationToken cancellationToken = default);
    Task<ChangeRequestDto> RequestApprovalAsync(Guid id, RequestDepartmentApprovalRequest request, CancellationToken cancellationToken = default);
    Task<ChangeRequestDto> RecordDecisionAsync(Guid id, RecordApprovalDecisionRequest request, CancellationToken cancellationToken = default);
}
