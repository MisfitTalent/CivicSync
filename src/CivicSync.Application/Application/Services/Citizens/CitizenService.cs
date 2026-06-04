using CivicSync.Node.Api.Application.Configuration;
using CivicSync.Node.Api.Contracts.Citizens;
using CivicSync.Node.Api.Domain.Citizens;
using CivicSync.Node.Api.Domain.Enums;
using CivicSync.Node.Api.Domain.Nodes;
using CivicSync.Node.Api.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Node.Api.Application.Services.Citizens;

public sealed class CitizenService : ICitizenService
{
    private readonly IRepository<Citizen, Guid> _citizenRepository;
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly NodeOptions _nodeOptions;

    public CitizenService(
        IRepository<Citizen, Guid> citizenRepository,
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _citizenRepository = citizenRepository;
        _departmentNodeRepository = departmentNodeRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task<CitizenDto> CreateAsync(CreateCitizenRequest request, CancellationToken cancellationToken = default)
    {
        var departmentNode = await GetLocalDepartmentNodeAsync(cancellationToken);
        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizenExists = await citizens.AnyAsync(
            item => item.DepartmentNodeId == departmentNode.Id && item.NationalIdNumber == request.NationalIdNumber,
            cancellationToken);

        if (citizenExists)
        {
            throw new InvalidOperationException("A citizen with the same national ID already exists on this node.");
        }

        var citizen = new Citizen(
            departmentNode.Id,
            request.NationalIdNumber,
            new PersonName(request.FirstName, request.LastName),
            new ContactDetails(request.EmailAddress, request.PhoneNumber));

        await _citizenRepository.InsertAsync(citizen, autoSave: true, cancellationToken);

        return MapToDto(citizen);
    }

    public async Task<IReadOnlyCollection<CitizenDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departmentNode = await GetLocalDepartmentNodeAsync(cancellationToken);
        var citizens = await _citizenRepository.GetQueryableAsync();

        return await citizens
            .Where(item => item.DepartmentNodeId == departmentNode.Id)
            .OrderBy(item => item.NationalIdNumber)
            .Select(item => MapToDto(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<CitizenDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizen = await citizens.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return citizen is null ? null : MapToDto(citizen);
    }

    private async Task<DepartmentNode> GetLocalDepartmentNodeAsync(CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        return await departmentNodes.SingleAsync(item => item.DepartmentCode == _nodeOptions.DepartmentCode, cancellationToken);
    }

    private static CitizenDto MapToDto(Citizen citizen)
    {
        return new CitizenDto
        {
            Id = citizen.Id,
            DepartmentNodeId = citizen.DepartmentNodeId,
            NationalIdNumber = citizen.NationalIdNumber,
            FirstName = citizen.FullName.FirstName,
            LastName = citizen.FullName.LastName,
            DisplayName = citizen.FullName.DisplayName,
            EmailAddress = citizen.ContactDetails.EmailAddress,
            PhoneNumber = citizen.ContactDetails.PhoneNumber,
            Status = citizen.Status,
            RecordVersion = citizen.RecordVersion,
            CreatedAtUtc = citizen.CreatedAtUtc
        };
    }
}
