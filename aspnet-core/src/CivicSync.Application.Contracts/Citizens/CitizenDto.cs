using CivicSync.Core.Domain.Enums;

namespace CivicSync.Application.Contracts.Citizens;

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
    public string DateOfBirth { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string BiometricReference { get; set; } = string.Empty;
    public string RelationshipStatus { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string EmploymentHistory { get; set; } = string.Empty;
    public string IncomeAndInvestmentProfile { get; set; } = string.Empty;
    public string BankingAndAssets { get; set; } = string.Empty;
    public string ResidentialAddress { get; set; } = string.Empty;
    public string RatesAccount { get; set; } = string.Empty;
    public string MunicipalServiceStatus { get; set; } = string.Empty;
    public CitizenStatus Status { get; set; }
    public long RecordVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyCollection<string> RedactedFields { get; set; } = [];
}
