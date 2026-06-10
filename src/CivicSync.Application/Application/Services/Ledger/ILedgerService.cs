using CivicSync.Application.Contracts.Ledger;

namespace CivicSync.Application.Services.Ledger;

public interface ILedgerService
{
    Task<CommitChangeRequestResponse> CommitChangeRequestAsync(Guid changeRequestId, CancellationToken cancellationToken = default);

    Task<ProcessApprovedChangeRequestsResponse> ProcessApprovedChangeRequestsAsync(
        int maxItems,
        CancellationToken cancellationToken = default);
}
