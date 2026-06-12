using CivicSync.Application.Services.Citizens;
using CivicSync.Application.Contracts.Citizens;
using Microsoft.AspNetCore.Mvc;

namespace CivicSync.Web.Host.Controllers;

[ApiController]
[Route("api/citizens")]
public sealed class CitizensController : ControllerBase
{
    private readonly ICitizenService _citizenService;

    public CitizensController(ICitizenService citizenService)
    {
        _citizenService = citizenService;
    }

    [HttpPost]
    public async Task<ActionResult<CitizenDto>> CreateAsync(
        CreateCitizenRequest request,
        CancellationToken cancellationToken)
    {
        var citizen = await _citizenService.CreateAsync(request, cancellationToken);

        return Created($"/api/citizens/{citizen.Id}", citizen);
    }

    [HttpPost("{id:guid}/biometrics/enroll")]
    public async Task<ActionResult<CitizenDto>> EnrollBiometricAsync(Guid id, BiometricEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var citizen = await _citizenService.EnrollBiometricAsync(id, request, cancellationToken);
        return Ok(citizen);
    }

    [HttpPost("{id:guid}/biometrics/verify")]
    public async Task<ActionResult<BiometricVerificationResult>> VerifyBiometricAsync(Guid id, BiometricVerificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _citizenService.VerifyBiometricAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CitizenDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var citizens = await _citizenService.GetAllAsync(cancellationToken);

        return Ok(citizens);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CitizenDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var citizen = await _citizenService.GetByIdAsync(id, cancellationToken);

        return citizen is null ? NotFound() : Ok(citizen);
    }
}
