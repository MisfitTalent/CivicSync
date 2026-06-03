using CivicSync.Node.Api.Contracts.Sync;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Application.Services.Sync;

public interface INodeSyncSignatureService
{
    string CreateSignature(
        ReceiveLedgerEntryRequest request,
        DepartmentCode sendingDepartmentCode,
        string timestampUtc,
        string sharedSecret);

    bool IsValidSignature(
        ReceiveLedgerEntryRequest request,
        DepartmentCode sendingDepartmentCode,
        string timestampUtc,
        string providedSignature,
        string sharedSecret);
}
