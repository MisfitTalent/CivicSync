using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CivicSync.Application.Contracts.Auth;
using CivicSync.Core.Configuration;
using CivicSync.Core.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Auth;

public sealed class PasskeyAuthService : IPasskeyAuthService
{
    private const string RegistrationPurpose = "registration";
    private const string LoginPurpose = "login";
    private const string RegistrationClientDataType = "webauthn.create";
    private const string LoginClientDataType = "webauthn.get";
    private const int ChallengeSizeBytes = 32;
    private const int TimeoutMs = 60000;
    private readonly IRepository<PasskeyCredential, Guid> _credentialRepository;
    private readonly IRepository<PasskeyChallenge, Guid> _challengeRepository;
    private readonly PasskeyOptions _passkeyOptions;
    private readonly HashSet<string> _allowedOrigins;

    public PasskeyAuthService(
        IRepository<PasskeyCredential, Guid> credentialRepository,
        IRepository<PasskeyChallenge, Guid> challengeRepository,
        IOptions<PasskeyOptions> passkeyOptions)
    {
        _credentialRepository = credentialRepository;
        _challengeRepository = challengeRepository;
        _passkeyOptions = passkeyOptions.Value;
        _allowedOrigins = _passkeyOptions.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<PasskeyChallengeResponse> BeginRegistrationAsync(
        BeginPasskeyRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await CreateChallengeResponseAsync(
            NormalizeEmail(request.EmailAddress),
            request.DisplayName.Trim(),
            RegistrationPurpose,
            includeCredentials: false,
            cancellationToken);
    }

    public async Task<PasskeyAuthenticationResult> CompleteRegistrationAsync(
        CompletePasskeyRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var emailAddress = NormalizeEmail(request.EmailAddress);
        var clientData = await DecodeAndValidateClientDataAsync(
            request.ClientDataJson,
            RegistrationClientDataType,
            emailAddress,
            RegistrationPurpose,
            cancellationToken);
        var credentialId = NormalizeRequiredBase64Url(request.CredentialId, "Credential ID");
        var publicKey = NormalizeRequiredBase64Url(request.PublicKey, "Public key");

        var credentials = await _credentialRepository.GetQueryableAsync();
        var existingCredential = await credentials.SingleOrDefaultAsync(
            item => item.CredentialId == credentialId,
            cancellationToken);

        if (existingCredential is null)
        {
            await _credentialRepository.InsertAsync(new PasskeyCredential(
                emailAddress,
                credentialId,
                publicKey,
                request.PublicKeyAlgorithm,
                emailAddress), autoSave: true, cancellationToken);
        }
        else
        {
            existingCredential.EmailAddress = emailAddress;
            existingCredential.PublicKey = publicKey;
            existingCredential.PublicKeyAlgorithm = request.PublicKeyAlgorithm;
            existingCredential.DisplayName = emailAddress;
            await _credentialRepository.UpdateAsync(existingCredential, autoSave: true, cancellationToken);
        }

        return new PasskeyAuthenticationResult
        {
            IsAuthenticated = true,
            EmailAddress = clientData.EmailAddress,
            Message = "Passkey registration verified by server challenge."
        };
    }

    public async Task<PasskeyChallengeResponse> BeginLoginAsync(
        BeginPasskeyLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var emailAddress = NormalizeEmail(request.EmailAddress);
        var credentials = await _credentialRepository.GetQueryableAsync();
        var credentialIds = await credentials
            .Where(item => item.EmailAddress == emailAddress)
            .Select(item => item.CredentialId)
            .ToListAsync(cancellationToken);

        if (credentialIds.Count == 0)
        {
            throw new InvalidOperationException("No passkey is registered for this account.");
        }

        return await CreateChallengeResponseAsync(
            emailAddress,
            emailAddress,
            LoginPurpose,
            includeCredentials: true,
            cancellationToken,
            credentialIds);
    }

    public async Task<PasskeyAuthenticationResult> CompleteLoginAsync(
        CompletePasskeyLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var emailAddress = NormalizeEmail(request.EmailAddress);
        _ = await DecodeAndValidateClientDataAsync(
            request.ClientDataJson,
            LoginClientDataType,
            emailAddress,
            LoginPurpose,
            cancellationToken);
        var credentialId = NormalizeRequiredBase64Url(request.CredentialId, "Credential ID");
        var authenticatorData = DecodeBase64Url(request.AuthenticatorData, "Authenticator data");
        var signature = DecodeBase64Url(request.Signature, "Signature");

        ValidateAuthenticatorData(authenticatorData);

        var credentials = await _credentialRepository.GetQueryableAsync();
        var credential = await credentials.SingleOrDefaultAsync(
            item => item.EmailAddress == emailAddress && item.CredentialId == credentialId,
            cancellationToken)
            ?? throw new InvalidOperationException("Passkey credential was not found for this account.");

        if (!VerifySignature(credential, authenticatorData, request.ClientDataJson, signature))
        {
            throw new InvalidOperationException("Passkey signature verification failed.");
        }

        credential.RecordSuccessfulAuthentication(ReadSignCount(authenticatorData));
        await _credentialRepository.UpdateAsync(credential, autoSave: true, cancellationToken);

        return new PasskeyAuthenticationResult
        {
            IsAuthenticated = true,
            EmailAddress = emailAddress,
            Message = "Passkey signature verified by backend."
        };
    }

    private async Task<PasskeyChallengeResponse> CreateChallengeResponseAsync(
        string emailAddress,
        string displayName,
        string purpose,
        bool includeCredentials,
        CancellationToken cancellationToken,
        IReadOnlyCollection<string>? credentialIds = null)
    {
        var challenge = CreateChallenge();
        await _challengeRepository.InsertAsync(new PasskeyChallenge(
            emailAddress,
            challenge,
            purpose,
            DateTime.UtcNow.AddMilliseconds(TimeoutMs)), autoSave: true, cancellationToken);

        return new PasskeyChallengeResponse
        {
            Challenge = challenge,
            RpId = _passkeyOptions.RelyingPartyId,
            RpName = _passkeyOptions.RelyingPartyName,
            UserId = Base64UrlEncode(Encoding.UTF8.GetBytes(emailAddress)),
            UserName = emailAddress,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? emailAddress : displayName,
            TimeoutMs = TimeoutMs,
            AllowedCredentialIds = includeCredentials ? credentialIds ?? [] : []
        };
    }

    private async Task<ServerClientData> DecodeAndValidateClientDataAsync(
        string clientDataJson,
        string expectedType,
        string emailAddress,
        string purpose,
        CancellationToken cancellationToken)
    {
        var clientDataBytes = DecodeBase64Url(clientDataJson, "Client data JSON");
        using var document = JsonDocument.Parse(clientDataBytes);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString();
        var challenge = root.GetProperty("challenge").GetString();
        var origin = root.GetProperty("origin").GetString();

        if (!string.Equals(type, expectedType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passkey client data type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(challenge))
        {
            throw new InvalidOperationException("Passkey challenge is missing.");
        }

        if (string.IsNullOrWhiteSpace(origin) || !_allowedOrigins.Contains(origin.Trim().TrimEnd('/')))
        {
            throw new InvalidOperationException("Passkey client origin is not allowed.");
        }

        await ValidateChallengeAsync(emailAddress, purpose, challenge, cancellationToken);

        return new ServerClientData(emailAddress, challenge);
    }

    private async Task ValidateChallengeAsync(
        string emailAddress,
        string purpose,
        string challenge,
        CancellationToken cancellationToken)
    {
        var challenges = await _challengeRepository.GetQueryableAsync();
        var matchingChallenge = await challenges
            .Where(item => item.EmailAddress == emailAddress && item.Purpose == purpose && item.Challenge == challenge)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Passkey challenge was not issued by this server.");

        if (!matchingChallenge.IsAvailable(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Passkey challenge expired or was already used.");
        }

        matchingChallenge.MarkUsed();
        await _challengeRepository.UpdateAsync(matchingChallenge, autoSave: true, cancellationToken);
    }

    private static bool VerifySignature(
        PasskeyCredential credential,
        byte[] authenticatorData,
        string clientDataJson,
        byte[] signature)
    {
        var clientDataHash = SHA256.HashData(DecodeBase64Url(clientDataJson, "Client data JSON"));
        var signedData = new byte[authenticatorData.Length + clientDataHash.Length];
        Buffer.BlockCopy(authenticatorData, 0, signedData, 0, authenticatorData.Length);
        Buffer.BlockCopy(clientDataHash, 0, signedData, authenticatorData.Length, clientDataHash.Length);
        var publicKey = DecodeBase64Url(credential.PublicKey, "Public key");

        if (credential.PublicKeyAlgorithm == -7)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }

        if (credential.PublicKeyAlgorithm == -257)
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        throw new InvalidOperationException("Unsupported passkey public key algorithm.");
    }

    private static void ValidateAuthenticatorData(byte[] authenticatorData)
    {
        if (authenticatorData.Length < 37)
        {
            throw new InvalidOperationException("Passkey authenticator data is invalid.");
        }

        var flags = authenticatorData[32];
        var userPresent = (flags & 0x01) == 0x01;
        var userVerified = (flags & 0x04) == 0x04;

        if (!userPresent || !userVerified)
        {
            throw new InvalidOperationException("Passkey user verification was not confirmed by the authenticator.");
        }
    }

    private static uint ReadSignCount(byte[] authenticatorData)
    {
        if (authenticatorData.Length < 37)
        {
            return 0;
        }

        return ((uint)authenticatorData[33] << 24) |
            ((uint)authenticatorData[34] << 16) |
            ((uint)authenticatorData[35] << 8) |
            authenticatorData[36];
    }

    private static string CreateChallenge()
    {
        var bytes = RandomNumberGenerator.GetBytes(ChallengeSizeBytes);
        return Base64UrlEncode(bytes);
    }

    private static string NormalizeEmail(string emailAddress)
    {
        var normalized = emailAddress.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Email address is required.");
        }

        return normalized;
    }

    private static string NormalizeRequiredBase64Url(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        _ = DecodeBase64Url(value, label);
        return value.Trim();
    }

    private static byte[] DecodeBase64Url(string value, string label)
    {
        try
        {
            var normalized = value.Trim().Replace('-', '+').Replace('_', '/');
            var padded = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            return Convert.FromBase64String(padded);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{label} must be valid base64url.");
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private sealed record ServerClientData(string EmailAddress, string Challenge);
}
