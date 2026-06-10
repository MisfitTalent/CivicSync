using CivicSync.Core.Domain.Common;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.ValueObjects;

namespace CivicSync.Core.Domain.Citizens;

public sealed class Citizen : EntityBase
{
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
    public CitizenStatus Status { get; set; }
    public long RecordVersion { get; set; }

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
