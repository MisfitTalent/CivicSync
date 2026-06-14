using CivicSync.Application.Contracts.Auth;
using CivicSync.Application.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Host.Controllers;

[ApiController]
[Route("api/auth/passkeys")]
public sealed class AuthController : ControllerBase
{
    private readonly IPasskeyAuthService _passkeyAuthService;

    public AuthController(IPasskeyAuthService passkeyAuthService)
    {
        _passkeyAuthService = passkeyAuthService;
    }

    [HttpPost("registration/options")]
    public async Task<ActionResult<PasskeyChallengeResponse>> BeginRegistrationAsync(
        BeginPasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _passkeyAuthService.BeginRegistrationAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("registration/verify")]
    public async Task<ActionResult<PasskeyAuthenticationResult>> CompleteRegistrationAsync(
        CompletePasskeyRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _passkeyAuthService.CompleteRegistrationAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("login/options")]
    public async Task<ActionResult<PasskeyChallengeResponse>> BeginLoginAsync(
        BeginPasskeyLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _passkeyAuthService.BeginLoginAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("login/verify")]
    public async Task<ActionResult<PasskeyAuthenticationResult>> CompleteLoginAsync(
        CompletePasskeyLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _passkeyAuthService.CompleteLoginAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
