using CivicSync.Application.Contracts.Sync;
using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Services.Sync;

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
