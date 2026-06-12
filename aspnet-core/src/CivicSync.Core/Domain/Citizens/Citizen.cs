using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.ValueObjects;

namespace CivicSync.Core.Domain.Citizens;

public sealed class Citizen : EntityBase
{
    private const int MaxBiometricDescriptorLength = 1200;

    private Citizen()
    {
    }

    public Citizen(Guid departmentNodeId, string nationalIdNumber, PersonName fullName, ContactDetails contactDetails)
    {
        DepartmentNodeId = departmentNodeId;
        NationalIdNumber = nationalIdNumber;
        FullName = fullName;
        ContactDetails = contactDetails;
        Status = CitizenStatus.Active;
        RecordVersion = 1;
    }

    public Guid DepartmentNodeId { get; set; }
    public string NationalIdNumber { get; set; } = string.Empty;
    public PersonName FullName { get; set; } = new(string.Empty, string.Empty);
    public ContactDetails ContactDetails { get; set; } = new(string.Empty, string.Empty);
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

    public void EnrollBiometric(string method, string deviceLabel, string descriptor)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new InvalidOperationException("A biometric method is required.");
        }

        if (string.IsNullOrWhiteSpace(descriptor))
        {
            throw new InvalidOperationException("A biometric face descriptor is required.");
        }

        var normalizedDescriptor = descriptor.Trim();
        if (normalizedDescriptor.Length > MaxBiometricDescriptorLength)
        {
            throw new InvalidOperationException("The biometric face descriptor is too large.");
        }

        var resolvedDeviceLabel = string.IsNullOrWhiteSpace(deviceLabel)
            ? "Registered browser camera"
            : deviceLabel.Trim();

        BiometricReference = $"{method.Trim()} enrolled on {resolvedDeviceLabel}|{normalizedDescriptor}";
        RecordVersion++;
        MarkUpdated();
    }

    public void ApplySharedFieldChange(string fieldName, string newValue)
    {
        switch (fieldName.Trim())
        {
            case nameof(FullName):
                FullName = ParseName(newValue);
                break;
            case nameof(ContactDetails):
                ContactDetails = ParseContactDetails(newValue);
                break;
            case nameof(NationalIdNumber):
                NationalIdNumber = newValue;
                break;
            case nameof(DateOfBirth):
                DateOfBirth = newValue;
                break;
            case nameof(PassportNumber):
                PassportNumber = newValue;
                break;
            case nameof(BiometricReference):
                BiometricReference = newValue;
                break;
            case nameof(RelationshipStatus):
                RelationshipStatus = newValue;
                break;
            case nameof(TaxNumber):
                TaxNumber = newValue;
                break;
            case nameof(EmploymentHistory):
                EmploymentHistory = newValue;
                break;
            case nameof(IncomeAndInvestmentProfile):
                IncomeAndInvestmentProfile = newValue;
                break;
            case nameof(BankingAndAssets):
                BankingAndAssets = newValue;
                break;
            case nameof(ResidentialAddress):
                ResidentialAddress = newValue;
                break;
            case nameof(RatesAccount):
                RatesAccount = newValue;
                break;
            case nameof(MunicipalServiceStatus):
                MunicipalServiceStatus = newValue;
                break;
            default:
                throw new InvalidOperationException($"Field '{fieldName}' is not a supported shared citizen field.");
        }

        RecordVersion++;
        MarkUpdated();
    }

    private static PersonName ParseName(string value)
    {
        var nameParts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return nameParts.Length == 1
            ? new PersonName(nameParts[0], string.Empty)
            : new PersonName(nameParts[0], nameParts[1]);
    }

    private static ContactDetails ParseContactDetails(string value)
    {
        var contactParts = value.Split('|', 2, StringSplitOptions.TrimEntries);
        return contactParts.Length == 1
            ? new ContactDetails(contactParts[0], string.Empty)
            : new ContactDetails(contactParts[0], contactParts[1]);
    }
}
