using CivicSync.Application.Contracts.Auth;

namespace CivicSync.Application.Services.Auth;

public interface IPasskeyAuthService
{
    Task<PasskeyChallengeResponse> BeginRegistrationAsync(
        BeginPasskeyRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<PasskeyAuthenticationResult> CompleteRegistrationAsync(
        CompletePasskeyRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<PasskeyChallengeResponse> BeginLoginAsync(
        BeginPasskeyLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<PasskeyAuthenticationResult> CompleteLoginAsync(
        CompletePasskeyLoginRequest request,
        CancellationToken cancellationToken = default);
}
