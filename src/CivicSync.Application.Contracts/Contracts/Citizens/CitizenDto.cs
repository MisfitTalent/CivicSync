using CivicSync.Node.Api.Domain.Enums;

namespace CivicSync.Node.Api.Contracts.Citizens;

public sealed class CitizenDto
{
    public Guid Id { get; set; }
    public Guid DepartmentNodeId { get; set; }
    public string NationalIdNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public CitizenStatus Status { get; set; }
    public long RecordVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
