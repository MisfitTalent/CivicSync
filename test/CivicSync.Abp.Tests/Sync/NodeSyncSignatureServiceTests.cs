using CivicSync.Node.Api.Application.Services.Sync;
using CivicSync.Node.Api.Contracts.Sync;
using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Tests.Sync;

public sealed class NodeSyncSignatureServiceTests
{
    private readonly NodeSyncSignatureService _signatureService = new();

    [Fact]
    public void IsValidSignature_ReturnsTrue_WhenPayloadAndSecretMatch()
    {
        var request = CreateRequest();
        var timestampUtc = DateTimeOffset.UtcNow.ToString("O");
        var signature = _signatureService.CreateSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            "test-secret");

        var isValid = _signatureService.IsValidSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            signature,
            "test-secret");

        Assert.True(isValid);
    }

    [Fact]
    public void IsValidSignature_ReturnsFalse_WhenPayloadIsChanged()
    {
        var request = CreateRequest();
        var timestampUtc = DateTimeOffset.UtcNow.ToString("O");
        var signature = _signatureService.CreateSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            "test-secret");

        request.FieldChanges[0].NewValue = "attacker@example.test|+27000000000";

        var isValid = _signatureService.IsValidSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            signature,
            "test-secret");

        Assert.False(isValid);
    }

    [Fact]
    public void IsValidSignature_ReturnsFalse_WhenTimestampIsTooOld()
    {
        var request = CreateRequest();
        var timestampUtc = DateTimeOffset.UtcNow.AddMinutes(-10).ToString("O");
        var signature = _signatureService.CreateSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            "test-secret");

        var isValid = _signatureService.IsValidSignature(
            request,
            DepartmentCode.HomeAffairs,
            timestampUtc,
            signature,
            "test-secret");

        Assert.False(isValid);
    }

    private static ReceiveLedgerEntryRequest CreateRequest()
    {
        return new ReceiveLedgerEntryRequest
        {
            LedgerEntryId = Guid.NewGuid(),
            OriginatingNodeId = Guid.NewGuid(),
            ChangeRequestId = Guid.NewGuid(),
            SequenceNumber = 10,
            EventType = LedgerEventType.ChangeCommitted,
            PayloadProofHash = "payload-proof",
            PreviousProofHash = "previous-proof",
            CurrentProofHash = "current-proof",
            CitizenNationalIdNumber = "9001015009087",
            FieldChanges =
            [
                new SyncedFieldChangeDto
                {
                    FieldName = "ContactDetails",
                    NewValue = "valid@example.test|+27820000000"
                }
            ]
        };
    }
}

