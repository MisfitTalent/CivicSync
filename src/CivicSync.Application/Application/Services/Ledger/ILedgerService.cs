using CivicSync.Node.Api.Contracts.Ledger;

namespace CivicSync.Node.Api.Application.Services.Ledger;

public interface ILedgerService
{
    Task<CommitChangeRequestResponse> CommitChangeRequestAsync(Guid changeRequestId, CancellationToken cancellationToken = default);
}
