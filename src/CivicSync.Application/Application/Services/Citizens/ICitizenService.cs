using CivicSync.Node.Api.Contracts.Citizens;

namespace CivicSync.Node.Api.Application.Services.Citizens;

public interface ICitizenService
{
    Task<CitizenDto> CreateAsync(CreateCitizenRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CitizenDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CitizenDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
