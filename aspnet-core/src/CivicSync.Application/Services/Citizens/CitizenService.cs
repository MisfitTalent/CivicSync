using CivicSync.Application.Contracts.Citizens;
using CivicSync.Core.Configuration;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using CivicSync.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.Citizens;

public sealed class CitizenService : ICitizenService
{
    private const int FaceTemplateTolerance = 82;
    private const int FaceEmbeddingDimensions = 128;
    private const double FaceEmbeddingDistanceThreshold = 0.6d;
    private const string FaceEmbeddingDescriptorPrefix = "face-api-recognition-v1:";
    private const string LegacyFaceTemplateDescriptorPrefix = "face-v1:";

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
            new ContactDetails(request.EmailAddress, request.PhoneNumber))
        {
            DateOfBirth = request.DateOfBirth,
            PassportNumber = request.PassportNumber,
            BiometricReference = request.BiometricReference,
            RelationshipStatus = request.RelationshipStatus,
            TaxNumber = request.TaxNumber,
            EmploymentHistory = request.EmploymentHistory,
            IncomeAndInvestmentProfile = request.IncomeAndInvestmentProfile,
            BankingAndAssets = request.BankingAndAssets,
            ResidentialAddress = request.ResidentialAddress,
            RatesAccount = request.RatesAccount,
            MunicipalServiceStatus = request.MunicipalServiceStatus
        };

        await _citizenRepository.InsertAsync(citizen, autoSave: true, cancellationToken);

        return MapToDto(citizen, _nodeOptions.DepartmentCode);
    }

    public async Task<CitizenDto> EnrollBiometricAsync(
        Guid id,
        BiometricEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var citizen = await _citizenRepository.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (citizen is null)
        {
            throw new InvalidOperationException("Citizen does not exist on this node.");
        }

        citizen.EnrollBiometric(request.Method, request.DeviceLabel, request.Descriptor);
        await _citizenRepository.UpdateAsync(citizen, autoSave: true, cancellationToken);

        return MapToDto(citizen, _nodeOptions.DepartmentCode);
    }

    public async Task<BiometricVerificationResult> VerifyBiometricAsync(
        Guid id,
        BiometricVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var citizen = await _citizenRepository.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (citizen is null)
        {
            throw new InvalidOperationException("Citizen does not exist on this node.");
        }

        var enrolledReference = citizen.BiometricReference;
        var hasEnrollment = !string.IsNullOrWhiteSpace(enrolledReference);
        var enrolledLabel = GetBiometricLabel(enrolledReference);
        var enrolledDescriptor = GetBiometricDescriptor(enrolledReference);
        var requestedMethod = request.Method?.Trim();
        var methodMatches = string.IsNullOrWhiteSpace(requestedMethod)
            || enrolledLabel.Contains(requestedMethod, StringComparison.OrdinalIgnoreCase);
        var descriptorMatches = DoesFaceEmbeddingMatch(enrolledDescriptor, request.Descriptor);
        var isVerified = hasEnrollment && methodMatches && descriptorMatches;

        return new BiometricVerificationResult
        {
            CitizenId = citizen.Id,
            IsVerified = isVerified,
            Message = isVerified
                ? "Face verification matched the enrolled biometric reference."
                : "Face verification failed because the captured face does not match the enrolled biometric reference.",
            VerifiedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyCollection<CitizenDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departmentNode = await GetLocalDepartmentNodeAsync(cancellationToken);
        var citizens = await _citizenRepository.GetQueryableAsync();

        return await citizens
            .Where(item => item.DepartmentNodeId == departmentNode.Id)
            .OrderBy(item => item.NationalIdNumber)
            .Select(item => MapToDto(item, _nodeOptions.DepartmentCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<CitizenDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizen = await citizens.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return citizen is null ? null : MapToDto(citizen, _nodeOptions.DepartmentCode);
    }

    private async Task<DepartmentNode> GetLocalDepartmentNodeAsync(CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        return await departmentNodes.SingleAsync(item => item.DepartmentCode == _nodeOptions.DepartmentCode, cancellationToken);
    }

    private static string GetBiometricLabel(string? biometricReference)
    {
        if (string.IsNullOrWhiteSpace(biometricReference))
        {
            return string.Empty;
        }

        return biometricReference.Split('|', 2)[0].Trim();
    }

    private static string GetBiometricDescriptor(string? biometricReference)
    {
        if (string.IsNullOrWhiteSpace(biometricReference))
        {
            return string.Empty;
        }

        var parts = biometricReference.Split('|', 2);
        return parts.Length == 2 ? parts[1].Trim() : string.Empty;
    }

    private static bool DoesFaceEmbeddingMatch(string enrolledDescriptor, string capturedDescriptor)
    {
        if (IsFaceEmbeddingDescriptor(enrolledDescriptor) || IsFaceEmbeddingDescriptor(capturedDescriptor))
        {
            return DoesFaceApiEmbeddingMatch(enrolledDescriptor, capturedDescriptor);
        }

        return DoesLegacyFaceTemplateMatch(enrolledDescriptor, capturedDescriptor);
    }

    private static bool DoesFaceApiEmbeddingMatch(string enrolledDescriptor, string capturedDescriptor)
    {
        try
        {
            var enrolledEmbedding = DecodeFaceEmbedding(enrolledDescriptor);
            var capturedEmbedding = DecodeFaceEmbedding(capturedDescriptor);

            if (enrolledEmbedding.Length != FaceEmbeddingDimensions || capturedEmbedding.Length != enrolledEmbedding.Length)
            {
                return false;
            }

            var squaredDistance = 0d;
            for (var index = 0; index < enrolledEmbedding.Length; index++)
            {
                var difference = enrolledEmbedding[index] - capturedEmbedding[index];
                squaredDistance += difference * difference;
            }

            return Math.Sqrt(squaredDistance) <= FaceEmbeddingDistanceThreshold;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool DoesLegacyFaceTemplateMatch(string enrolledDescriptor, string capturedDescriptor)
    {
        try
        {
            var enrolledBytes = DecodeFaceTemplate(enrolledDescriptor);
            var capturedBytes = DecodeFaceTemplate(capturedDescriptor);

            if (enrolledBytes.Length == 0 || enrolledBytes.Length != capturedBytes.Length)
            {
                return false;
            }

            var totalDifference = 0;
            for (var index = 0; index < enrolledBytes.Length; index++)
            {
                totalDifference += Math.Abs(enrolledBytes[index] - capturedBytes[index]);
            }

            var averageDifference = totalDifference / enrolledBytes.Length;
            return averageDifference <= FaceTemplateTolerance;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsFaceEmbeddingDescriptor(string descriptor)
    {
        return !string.IsNullOrWhiteSpace(descriptor)
            && descriptor.Trim().StartsWith(FaceEmbeddingDescriptorPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static double[] DecodeFaceEmbedding(string descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor))
        {
            return Array.Empty<double>();
        }

        var normalizedDescriptor = descriptor.Trim();
        if (normalizedDescriptor.StartsWith(FaceEmbeddingDescriptorPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalizedDescriptor = normalizedDescriptor[FaceEmbeddingDescriptorPrefix.Length..];
        }

        var bytes = Convert.FromBase64String(normalizedDescriptor);
        if (bytes.Length != FaceEmbeddingDimensions * sizeof(float))
        {
            return Array.Empty<double>();
        }

        var embedding = new double[FaceEmbeddingDimensions];
        for (var index = 0; index < embedding.Length; index++)
        {
            embedding[index] = BitConverter.ToSingle(bytes, index * sizeof(float));
        }

        return embedding;
    }

    private static byte[] DecodeFaceTemplate(string descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor))
        {
            return Array.Empty<byte>();
        }

        var normalizedDescriptor = descriptor.Trim();
        if (normalizedDescriptor.StartsWith(LegacyFaceTemplateDescriptorPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalizedDescriptor = normalizedDescriptor[LegacyFaceTemplateDescriptorPrefix.Length..];
        }

        return Convert.FromBase64String(normalizedDescriptor);
    }

    private static CitizenDto MapToDto(Citizen citizen, DepartmentCode departmentCode)
    {
        var redactedFields = new List<string>();

        string Visible(string fieldName, string value)
        {
            if (CitizenFieldApprovalPolicy.CanDepartmentAccessField(departmentCode, fieldName))
            {
                return value;
            }

            redactedFields.Add(fieldName);
            return CitizenFieldApprovalPolicy.RedactedValue;
        }

        return new CitizenDto
        {
            Id = citizen.Id,
            DepartmentNodeId = citizen.DepartmentNodeId,
            NationalIdNumber = Visible(nameof(citizen.NationalIdNumber), citizen.NationalIdNumber),
            FirstName = Visible(nameof(citizen.FullName), citizen.FullName.FirstName),
            LastName = Visible(nameof(citizen.FullName), citizen.FullName.LastName),
            DisplayName = Visible(nameof(citizen.FullName), citizen.FullName.DisplayName),
            EmailAddress = Visible(nameof(citizen.ContactDetails), citizen.ContactDetails.EmailAddress),
            PhoneNumber = Visible(nameof(citizen.ContactDetails), citizen.ContactDetails.PhoneNumber),
            DateOfBirth = Visible(nameof(citizen.DateOfBirth), citizen.DateOfBirth),
            PassportNumber = Visible(nameof(citizen.PassportNumber), citizen.PassportNumber),
            BiometricReference = Visible(nameof(citizen.BiometricReference), citizen.BiometricReference),
            RelationshipStatus = Visible(nameof(citizen.RelationshipStatus), citizen.RelationshipStatus),
            TaxNumber = Visible(nameof(citizen.TaxNumber), citizen.TaxNumber),
            EmploymentHistory = Visible(nameof(citizen.EmploymentHistory), citizen.EmploymentHistory),
            IncomeAndInvestmentProfile = Visible(nameof(citizen.IncomeAndInvestmentProfile), citizen.IncomeAndInvestmentProfile),
            BankingAndAssets = Visible(nameof(citizen.BankingAndAssets), citizen.BankingAndAssets),
            ResidentialAddress = Visible(nameof(citizen.ResidentialAddress), citizen.ResidentialAddress),
            RatesAccount = Visible(nameof(citizen.RatesAccount), citizen.RatesAccount),
            MunicipalServiceStatus = Visible(nameof(citizen.MunicipalServiceStatus), citizen.MunicipalServiceStatus),
            Status = citizen.Status,
            RecordVersion = citizen.RecordVersion,
            CreatedAtUtc = citizen.CreatedAtUtc,
            RedactedFields = redactedFields.Distinct().ToList()
        };
    }
}
