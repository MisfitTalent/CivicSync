using System.Security.Cryptography;
using System.Text;
using CivicSync.Node.Api.Contracts.Sync;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Application.Services.Sync;

public sealed class NodeSyncSignatureService : INodeSyncSignatureService
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

    public string CreateSignature(
        ReceiveLedgerEntryRequest request,
        DepartmentCode sendingDepartmentCode,
        string timestampUtc,
        string sharedSecret)
    {
        var canonicalPayload = BuildCanonicalPayload(request, sendingDepartmentCode, timestampUtc);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalPayload));

        return Convert.ToBase64String(hash);
    }

    public bool IsValidSignature(
        ReceiveLedgerEntryRequest request,
        DepartmentCode sendingDepartmentCode,
        string timestampUtc,
        string providedSignature,
        string sharedSecret)
    {
        if (!DateTimeOffset.TryParse(timestampUtc, out var parsedTimestamp))
        {
            return false;
        }

        var age = DateTimeOffset.UtcNow - parsedTimestamp.ToUniversalTime();
        if (age.Duration() > AllowedClockSkew)
        {
            return false;
        }

        var expectedSignature = CreateSignature(request, sendingDepartmentCode, timestampUtc, sharedSecret);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        var providedBytes = Encoding.UTF8.GetBytes(providedSignature);

        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static string BuildCanonicalPayload(
        ReceiveLedgerEntryRequest request,
        DepartmentCode sendingDepartmentCode,
        string timestampUtc)
    {
        var fieldChanges = request.FieldChanges
            .OrderBy(item => item.FieldName, StringComparer.Ordinal)
            .ThenBy(item => item.NewValue, StringComparer.Ordinal)
            .Select(item => $"{item.FieldName.Trim()}={item.NewValue.Trim()}");

        return string.Join('|',
            sendingDepartmentCode,
            timestampUtc,
            request.LedgerEntryId,
            request.OriginatingNodeId,
            request.ChangeRequestId,
            request.SequenceNumber,
            request.EventType,
            request.PayloadProofHash,
            request.PreviousProofHash,
            request.CurrentProofHash,
            request.CitizenNationalIdNumber,
            string.Join(';', fieldChanges));
    }
}
