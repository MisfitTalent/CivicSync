using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicSync.Application.Contracts.Auth;
using CivicSync.Application.Services.Auth;
using CivicSync.Core.Configuration;
using CivicSync.Core.Domain.Auth;
using CivicSync.EntityFrameworkCore.Infrastructure.Persistence;
using CivicSync.Web.Host.Tests.TestSupport;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Tests.Services;

public sealed class PasskeyAuthServiceTests
{
    [Fact]
    public async Task CompleteRegistrationAsync_StoresCredential_WhenServerChallengeMatches()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);
        var options = await service.BeginRegistrationAsync(new BeginPasskeyRegistrationRequest
        {
            EmailAddress = "citizen@civicsync.local",
            DisplayName = "Citizen User"
        });

        var result = await service.CompleteRegistrationAsync(new CompletePasskeyRegistrationRequest
        {
            EmailAddress = "citizen@civicsync.local",
            CredentialId = Base64UrlEncode("credential-1"u8.ToArray()),
            ClientDataJson = CreateClientDataJson("webauthn.create", options.Challenge),
            PublicKey = Base64UrlEncode("fake-public-key"u8.ToArray()),
            PublicKeyAlgorithm = -7
        });

        Assert.True(result.IsAuthenticated);
        var credential = Assert.Single(dbContext.PasskeyCredentials.Local);
        Assert.Equal("citizen@civicsync.local", credential.EmailAddress);
        Assert.Equal(Base64UrlEncode("credential-1"u8.ToArray()), credential.CredentialId);
    }

    [Fact]
    public async Task CompleteLoginAsync_VerifiesWebAuthnSignature()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = ecdsa.ExportSubjectPublicKeyInfo();
        var credentialId = Base64UrlEncode("credential-1"u8.ToArray());
        dbContext.PasskeyCredentials.Add(new PasskeyCredential(
            "citizen@civicsync.local",
            credentialId,
            Base64UrlEncode(publicKey),
            -7,
            "Citizen User"));
        await Task.CompletedTask;
        var service = CreateService(dbContext);
        var options = await service.BeginLoginAsync(new BeginPasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local"
        });
        var clientDataJson = CreateClientDataJson("webauthn.get", options.Challenge);
        var authenticatorData = CreateAuthenticatorData(signCount: 9);
        var signature = SignAssertion(ecdsa, authenticatorData, clientDataJson);

        var result = await service.CompleteLoginAsync(new CompletePasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local",
            CredentialId = credentialId,
            ClientDataJson = clientDataJson,
            AuthenticatorData = Base64UrlEncode(authenticatorData),
            Signature = Base64UrlEncode(signature)
        });

        Assert.True(result.IsAuthenticated);
        Assert.Equal((uint)9, Assert.Single(dbContext.PasskeyCredentials.Local).SignCount);
    }

    [Fact]
    public async Task CompleteLoginAsync_Throws_WhenSignatureIsInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var registeredKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var attackerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = Base64UrlEncode("credential-1"u8.ToArray());
        dbContext.PasskeyCredentials.Add(new PasskeyCredential(
            "citizen@civicsync.local",
            credentialId,
            Base64UrlEncode(registeredKey.ExportSubjectPublicKeyInfo()),
            -7,
            "Citizen User"));
        await Task.CompletedTask;
        var service = CreateService(dbContext);
        var options = await service.BeginLoginAsync(new BeginPasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local"
        });
        var clientDataJson = CreateClientDataJson("webauthn.get", options.Challenge);
        var authenticatorData = CreateAuthenticatorData(signCount: 1);
        var invalidSignature = SignAssertion(attackerKey, authenticatorData, clientDataJson);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteLoginAsync(new CompletePasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local",
            CredentialId = credentialId,
            ClientDataJson = clientDataJson,
            AuthenticatorData = Base64UrlEncode(authenticatorData),
            Signature = Base64UrlEncode(invalidSignature)
        }));

        Assert.Equal("Passkey signature verification failed.", exception.Message);
    }

    [Fact]
    public async Task CompleteLoginAsync_Throws_WhenOriginIsNotAllowed()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = Base64UrlEncode("credential-1"u8.ToArray());
        dbContext.PasskeyCredentials.Add(new PasskeyCredential(
            "citizen@civicsync.local",
            credentialId,
            Base64UrlEncode(ecdsa.ExportSubjectPublicKeyInfo()),
            -7,
            "Citizen User"));
        await Task.CompletedTask;
        var service = CreateService(dbContext);
        var options = await service.BeginLoginAsync(new BeginPasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local"
        });
        var clientDataJson = CreateClientDataJson("webauthn.get", options.Challenge, "https://evil.example");
        var authenticatorData = CreateAuthenticatorData(signCount: 1);
        var signature = SignAssertion(ecdsa, authenticatorData, clientDataJson);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteLoginAsync(new CompletePasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local",
            CredentialId = credentialId,
            ClientDataJson = clientDataJson,
            AuthenticatorData = Base64UrlEncode(authenticatorData),
            Signature = Base64UrlEncode(signature)
        }));

        Assert.Equal("Passkey client origin is not allowed.", exception.Message);
    }

    [Fact]
    public async Task CompleteLoginAsync_Throws_WhenUserVerificationIsMissing()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credentialId = Base64UrlEncode("credential-1"u8.ToArray());
        dbContext.PasskeyCredentials.Add(new PasskeyCredential(
            "citizen@civicsync.local",
            credentialId,
            Base64UrlEncode(ecdsa.ExportSubjectPublicKeyInfo()),
            -7,
            "Citizen User"));
        await Task.CompletedTask;
        var service = CreateService(dbContext);
        var options = await service.BeginLoginAsync(new BeginPasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local"
        });
        var clientDataJson = CreateClientDataJson("webauthn.get", options.Challenge);
        var authenticatorData = CreateAuthenticatorData(signCount: 1, flags: 0x01);
        var signature = SignAssertion(ecdsa, authenticatorData, clientDataJson);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteLoginAsync(new CompletePasskeyLoginRequest
        {
            EmailAddress = "citizen@civicsync.local",
            CredentialId = credentialId,
            ClientDataJson = clientDataJson,
            AuthenticatorData = Base64UrlEncode(authenticatorData),
            Signature = Base64UrlEncode(signature)
        }));

        Assert.Equal("Passkey user verification was not confirmed by the authenticator.", exception.Message);
    }

    private static PasskeyAuthService CreateService(CivicSyncDbContext dbContext)
    {
        return new PasskeyAuthService(
            new TestRepository<PasskeyCredential>(dbContext),
            new TestRepository<PasskeyChallenge>(dbContext),
            Options.Create(new PasskeyOptions()));
    }

    private static string CreateClientDataJson(
        string type,
        string challenge,
        string origin = "http://localhost:5173")
    {
        var json = JsonSerializer.Serialize(new
        {
            type,
            challenge,
            origin
        });

        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    private static byte[] CreateAuthenticatorData(uint signCount, byte flags = 0x05)
    {
        var authenticatorData = new byte[37];
        RandomNumberGenerator.Fill(authenticatorData.AsSpan(0, 32));
        authenticatorData[32] = flags;
        authenticatorData[33] = (byte)(signCount >> 24);
        authenticatorData[34] = (byte)(signCount >> 16);
        authenticatorData[35] = (byte)(signCount >> 8);
        authenticatorData[36] = (byte)signCount;
        return authenticatorData;
    }

    private static byte[] SignAssertion(ECDsa ecdsa, byte[] authenticatorData, string clientDataJson)
    {
        var clientDataHash = SHA256.HashData(Base64UrlDecode(clientDataJson));
        var signedData = new byte[authenticatorData.Length + clientDataHash.Length];
        Buffer.BlockCopy(authenticatorData, 0, signedData, 0, authenticatorData.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedData, authenticatorData.Length, clientDataHash.Length);

        return ecdsa.SignData(signedData, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padded = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
