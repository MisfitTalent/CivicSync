using CivicSync.Core.Domain.Enums;

namespace CivicSync.Core.Domain.ChangeRequests;

public static class CitizenFieldApprovalPolicy
{
    private static readonly IReadOnlyDictionary<string, DepartmentCode[]> ApprovalDepartmentsByField =
        new Dictionary<string, DepartmentCode[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Normalize("FullName")] = [DepartmentCode.HomeAffairs],
            [Normalize("NationalIdNumber")] = [DepartmentCode.HomeAffairs],
            [Normalize("DateOfBirth")] = [DepartmentCode.HomeAffairs],
            [Normalize("PassportNumber")] = [DepartmentCode.HomeAffairs],
            [Normalize("BiometricReference")] = [DepartmentCode.HomeAffairs],
            [Normalize("RelationshipStatus")] = [DepartmentCode.HomeAffairs],
            [Normalize("ContactDetails")] = [DepartmentCode.HomeAffairs, DepartmentCode.Sars, DepartmentCode.Municipality],
            [Normalize("EmailAddress")] = [DepartmentCode.HomeAffairs, DepartmentCode.Sars, DepartmentCode.Municipality],
            [Normalize("PhoneNumber")] = [DepartmentCode.HomeAffairs, DepartmentCode.Sars, DepartmentCode.Municipality],
            [Normalize("TaxNumber")] = [DepartmentCode.Sars],
            [Normalize("EmploymentHistory")] = [DepartmentCode.Sars],
            [Normalize("IncomeAndInvestmentProfile")] = [DepartmentCode.Sars],
            [Normalize("BankingAndAssets")] = [DepartmentCode.Sars],
            [Normalize("ResidentialAddress")] = [DepartmentCode.Municipality],
            [Normalize("RatesAccount")] = [DepartmentCode.Municipality],
            [Normalize("MunicipalServiceStatus")] = [DepartmentCode.Municipality],
        };

    public static IReadOnlyCollection<DepartmentCode> GetRequiredApprovalDepartments(IEnumerable<string> fieldNames)
    {
        return fieldNames
            .SelectMany(GetRequiredApprovalDepartments)
            .Distinct()
            .ToList();
    }

    public static IReadOnlyCollection<DepartmentCode> GetRequiredApprovalDepartments(string fieldName)
    {
        var normalizedFieldName = Normalize(fieldName);

        return ApprovalDepartmentsByField.TryGetValue(normalizedFieldName, out var departmentCodes)
            ? departmentCodes
            : throw new InvalidOperationException($"Field '{fieldName}' is not configured for approval routing.");
    }

    private static string Normalize(string value)
    {
        return string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
    }
}
