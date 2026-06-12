using CivicSync.EntityFrameworkCore.Infrastructure.Security;

namespace CivicSync.Web.Host.Tests.Infrastructure;

public sealed class MasterRecordEncryptionTests
{
    [Fact]
    public void Encrypt_ReturnsUnreadablePayload_AndDecryptsOriginalValue()
    {
        const string purpose = "Citizen.NationalIdNumber";
        const string value = "9001015009087";

        var encryptedValue = MasterRecordEncryption.Encrypt(value, purpose);
        var decryptedValue = MasterRecordEncryption.Decrypt(encryptedValue, purpose);

        Assert.NotEqual(value, encryptedValue);
        Assert.DoesNotContain(value, encryptedValue);
        Assert.StartsWith("enc:v1:", encryptedValue);
        Assert.Equal(value, decryptedValue);
    }

    [Fact]
    public void Encrypt_ReturnsSamePayload_ForSameValueAndPurpose()
    {
        const string purpose = "Citizen.NationalIdNumber";
        const string value = "9001015009087";

        var firstEncryptedValue = MasterRecordEncryption.Encrypt(value, purpose);
        var secondEncryptedValue = MasterRecordEncryption.Encrypt(value, purpose);

        Assert.Equal(firstEncryptedValue, secondEncryptedValue);
    }

    [Fact]
    public void Decrypt_ReturnsPlaintext_WhenValueWasNotEncrypted()
    {
        const string plaintextValue = "legacy@example.test";

        var decryptedValue = MasterRecordEncryption.Decrypt(plaintextValue, "Citizen.EmailAddress");

        Assert.Equal(plaintextValue, decryptedValue);
    }
}
