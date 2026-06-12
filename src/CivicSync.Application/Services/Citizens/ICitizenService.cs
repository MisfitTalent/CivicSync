using CivicSync.Application.Contracts.Citizens;

namespace CivicSync.Application.Services.Citizens;

public interface ICitizenService
{
    Task<CitizenDto> CreateAsync(CreateCitizenRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CitizenDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CitizenDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
