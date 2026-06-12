using CivicSync.Core.Configuration;
using CivicSync.Application.Contracts.ChangeRequests;
using CivicSync.Core.Domain.ChangeRequests;
using CivicSync.Core.Domain.Citizens;
using CivicSync.Core.Domain.Enums;
using CivicSync.Core.Domain.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace CivicSync.Application.Services.ChangeRequests;

public sealed class ChangeRequestService : IChangeRequestService
{
    private readonly IRepository<ChangeRequest, Guid> _changeRequestRepository;
    private readonly IRepository<Citizen, Guid> _citizenRepository;
    private readonly IRepository<DepartmentApproval, Guid> _departmentApprovalRepository;
    private readonly IRepository<DepartmentNode, Guid> _departmentNodeRepository;
    private readonly IRepository<DepartmentUser, Guid> _departmentUserRepository;
    private readonly NodeOptions _nodeOptions;

    public ChangeRequestService(
        IRepository<ChangeRequest, Guid> changeRequestRepository,
        IRepository<Citizen, Guid> citizenRepository,
        IRepository<DepartmentApproval, Guid> departmentApprovalRepository,
        IRepository<DepartmentNode, Guid> departmentNodeRepository,
        IRepository<DepartmentUser, Guid> departmentUserRepository,
        IOptions<NodeOptions> nodeOptions)
    {
        _changeRequestRepository = changeRequestRepository;
        _citizenRepository = citizenRepository;
        _departmentApprovalRepository = departmentApprovalRepository;
        _departmentNodeRepository = departmentNodeRepository;
        _departmentUserRepository = departmentUserRepository;
        _nodeOptions = nodeOptions.Value;
    }

    public async Task<ChangeRequestDto> SubmitAsync(SubmitChangeRequest request, CancellationToken cancellationToken = default)
    {
        var departmentNode = await GetLocalDepartmentNodeAsync(cancellationToken);
        var citizens = await _citizenRepository.GetQueryableAsync();
        var citizen = await citizens.SingleOrDefaultAsync(
            item => item.Id == request.CitizenId && item.DepartmentNodeId == departmentNode.Id,
            cancellationToken);

        if (citizen is null)
        {
            throw new InvalidOperationException("Citizen does not exist on this node.");
        }

        var changeRequest = new ChangeRequest(departmentNode.Id, citizen.Id, request.Reason, citizen.RecordVersion);

        foreach (var fieldChange in request.FieldChanges)
        {
            ValidateRequesterCanChangeField(departmentNode.DepartmentCode, fieldChange.FieldName);

            var oldValue = GetCitizenFieldValue(citizen, fieldChange.FieldName);
            changeRequest.AddFieldChange(fieldChange.FieldName, oldValue, fieldChange.NewValue);
        }

        await RequestRequiredApprovalsAsync(
            changeRequest,
            request.FieldChanges.Select(item => item.FieldName),
            cancellationToken);

        await _changeRequestRepository.InsertAsync(changeRequest, autoSave: true, cancellationToken);

        return await GetRequiredByIdAsync(changeRequest.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ChangeRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var changeRequests = await GetChangeRequestsWithDetailsAsync();
        return await changeRequests
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => MapToDto(item, _nodeOptions.DepartmentCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChangeRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var changeRequest = await LoadChangeRequestAsync(id, cancellationToken);
        return changeRequest is null ? null : MapToDto(changeRequest, _nodeOptions.DepartmentCode);
    }

    public async Task<ChangeRequestDto> RequestApprovalAsync(
        Guid id,
        RequestDepartmentApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ApprovingNodeId == Guid.Empty)
        {
            throw new InvalidOperationException("Approving node is required.");
        }

        if (request.ApproverUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Approver user is required.");
        }

        var changeRequest = await LoadRequiredChangeRequestAsync(id, cancellationToken);
        var approvingNode = await GetRequiredDepartmentNodeAsync(request.ApprovingNodeId, cancellationToken);
        var approver = await GetRequiredApproverAsync(request.ApproverUserId, cancellationToken);

        ValidateApproverForNode(approver, approvingNode);

        var approvalAlreadyExists = changeRequest.Approvals.Any(item => item.ApprovingNodeId == approvingNode.Id);
        var approval = changeRequest.RequestApprovalFrom(
            approvingNode.Id,
            approver.Id,
            approver.FullName,
            approver.Role,
            approvingNode.DepartmentCode.ToString());

        if (!approvalAlreadyExists)
        {
            await _departmentApprovalRepository.InsertAsync(approval, autoSave: true, cancellationToken);
        }

        return MapToDto(changeRequest, _nodeOptions.DepartmentCode);
    }

    public async Task<ChangeRequestDto> RecordDecisionAsync(
        Guid id,
        RecordApprovalDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ApprovingNodeId == Guid.Empty)
        {
            throw new InvalidOperationException("Approving node is required.");
        }

        if (request.ApproverUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Approver user is required.");
        }

        var changeRequest = await LoadRequiredChangeRequestAsync(id, cancellationToken);
        var approvingNode = await GetRequiredDepartmentNodeAsync(request.ApprovingNodeId, cancellationToken);
        var approver = await GetRequiredApproverAsync(request.ApproverUserId, cancellationToken);

        ValidateApproverForNode(approver, approvingNode);
        ValidateApproverAssignedToApproval(changeRequest, approvingNode.Id, approver.Id);

        changeRequest.RecordDecision(approvingNode.Id, request.Decision, request.Comment);
        await _changeRequestRepository.UpdateAsync(changeRequest, autoSave: true, cancellationToken);

        return MapToDto(changeRequest, _nodeOptions.DepartmentCode);
    }

    private async Task RequestRequiredApprovalsAsync(
        ChangeRequest changeRequest,
        IEnumerable<string> fieldNames,
        CancellationToken cancellationToken)
    {
        var requiredDepartmentCodes = CitizenFieldApprovalPolicy.GetRequiredApprovalDepartments(fieldNames);
        if (requiredDepartmentCodes.Count == 0)
        {
            return;
        }

        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        var requiredNodes = await departmentNodes
            .Where(item => requiredDepartmentCodes.Contains(item.DepartmentCode))
            .ToListAsync(cancellationToken);

        var nodeIds = requiredNodes.Select(item => item.Id).ToList();
        var departmentUsers = await _departmentUserRepository.GetQueryableAsync();
        var activeApprovers = await departmentUsers
            .Where(item => nodeIds.Contains(item.DepartmentNodeId) && item.IsActive)
            .OrderBy(item => item.FullName)
            .ToListAsync(cancellationToken);

        foreach (var departmentCode in requiredDepartmentCodes)
        {
            var node = requiredNodes.SingleOrDefault(item => item.DepartmentCode == departmentCode)
                ?? throw new InvalidOperationException($"Required approval node '{departmentCode}' is not registered.");

            var approver = activeApprovers.FirstOrDefault(item => item.DepartmentNodeId == node.Id)
                ?? throw new InvalidOperationException($"Required approval node '{departmentCode}' has no active approver user.");

            changeRequest.RequestApprovalFrom(
                node.Id,
                approver.Id,
                approver.FullName,
                approver.Role,
                node.DepartmentCode.ToString());
        }
    }

    private static void ValidateApproverForNode(DepartmentUser approver, DepartmentNode approvingNode)
    {
        if (approver.DepartmentNodeId != approvingNode.Id)
        {
            throw new InvalidOperationException("Approver user does not belong to the approving node.");
        }

        if (!approver.IsActive)
        {
            throw new InvalidOperationException("Approver user is inactive.");
        }
    }

    private static void ValidateRequesterCanChangeField(DepartmentCode departmentCode, string fieldName)
    {
        if (!CitizenFieldApprovalPolicy.CanDepartmentRequestFieldChange(departmentCode, fieldName))
        {
            throw new InvalidOperationException(
                $"Department '{departmentCode}' is not allowed to request changes for field '{fieldName}'.");
        }
    }

    private static void ValidateApproverAssignedToApproval(
        ChangeRequest changeRequest,
        Guid approvingNodeId,
        Guid approverUserId)
    {
        var approval = changeRequest.Approvals.SingleOrDefault(item => item.ApprovingNodeId == approvingNodeId)
            ?? throw new InvalidOperationException("The selected node is not required to approve this change request.");

        if (approval.ApproverUserId != approverUserId)
        {
            throw new InvalidOperationException("Approver user is not assigned to this approval.");
        }
    }

    private async Task<DepartmentNode> GetRequiredDepartmentNodeAsync(Guid id, CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        return await departmentNodes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Approving node does not exist.");
    }

    private async Task<DepartmentUser> GetRequiredApproverAsync(Guid id, CancellationToken cancellationToken)
    {
        var departmentUsers = await _departmentUserRepository.GetQueryableAsync();
        return await departmentUsers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Approver user does not exist.");
    }

    private async Task<ChangeRequestDto> GetRequiredByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var changeRequest = await GetByIdAsync(id, cancellationToken);
        return changeRequest ?? throw new InvalidOperationException("Change request could not be loaded after saving.");
    }

    private async Task<ChangeRequest> LoadRequiredChangeRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        return await LoadChangeRequestAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Change request was not found.");
    }

    private async Task<ChangeRequest?> LoadChangeRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var changeRequests = await GetChangeRequestsWithDetailsAsync();
        return await changeRequests.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    private async Task<IQueryable<ChangeRequest>> GetChangeRequestsWithDetailsAsync()
    {
        return await _changeRequestRepository.WithDetailsAsync(item => item.FieldChanges, item => item.Approvals);
    }

    private async Task<DepartmentNode> GetLocalDepartmentNodeAsync(CancellationToken cancellationToken)
    {
        var departmentNodes = await _departmentNodeRepository.GetQueryableAsync();
        return await departmentNodes.SingleAsync(item => item.DepartmentCode == _nodeOptions.DepartmentCode, cancellationToken);
    }

    private static string GetCitizenFieldValue(Citizen citizen, string fieldName)
    {
        return fieldName.Trim() switch
        {
            nameof(citizen.NationalIdNumber) => citizen.NationalIdNumber,
            nameof(citizen.FullName) => citizen.FullName.DisplayName,
            nameof(citizen.ContactDetails) => $"{citizen.ContactDetails.EmailAddress}|{citizen.ContactDetails.PhoneNumber}",
            nameof(citizen.ContactDetails.EmailAddress) => citizen.ContactDetails.EmailAddress,
            nameof(citizen.ContactDetails.PhoneNumber) => citizen.ContactDetails.PhoneNumber,
            nameof(citizen.DateOfBirth) => citizen.DateOfBirth,
            nameof(citizen.PassportNumber) => citizen.PassportNumber,
            nameof(citizen.BiometricReference) => citizen.BiometricReference,
            nameof(citizen.RelationshipStatus) => citizen.RelationshipStatus,
            nameof(citizen.TaxNumber) => citizen.TaxNumber,
            nameof(citizen.EmploymentHistory) => citizen.EmploymentHistory,
            nameof(citizen.IncomeAndInvestmentProfile) => citizen.IncomeAndInvestmentProfile,
            nameof(citizen.BankingAndAssets) => citizen.BankingAndAssets,
            nameof(citizen.ResidentialAddress) => citizen.ResidentialAddress,
            nameof(citizen.RatesAccount) => citizen.RatesAccount,
            nameof(citizen.MunicipalServiceStatus) => citizen.MunicipalServiceStatus,
            _ => throw new InvalidOperationException($"Field '{fieldName}' is not a supported shared citizen field.")
        };
    }

    private static ChangeRequestDto MapToDto(ChangeRequest changeRequest, DepartmentCode departmentCode)
    {
        static string VisibleFieldValue(DepartmentCode departmentCode, FieldChange fieldChange, string value)
        {
            return CitizenFieldApprovalPolicy.RedactIfRestricted(departmentCode, fieldChange.FieldName, value);
        }

        return new ChangeRequestDto
        {
            Id = changeRequest.Id,
            RequestedAtNodeId = changeRequest.RequestedAtNodeId,
            CitizenId = changeRequest.CitizenId,
            Reason = changeRequest.Reason,
            ExpectedCitizenVersion = changeRequest.ExpectedCitizenVersion,
            CommittedCitizenVersion = changeRequest.CommittedCitizenVersion,
            Status = changeRequest.Status,
            CreatedAtUtc = changeRequest.CreatedAtUtc,
            FieldChanges = changeRequest.FieldChanges
                .Select(item => new FieldChangeDto
                {
                    Id = item.Id,
                    FieldName = item.FieldName,
                    OldValue = VisibleFieldValue(departmentCode, item, item.OldValue),
                    NewValue = VisibleFieldValue(departmentCode, item, item.NewValue)
                })
                .ToList(),
            Approvals = changeRequest.Approvals
                .Select(item => new DepartmentApprovalDto
                {
                    Id = item.Id,
                    ApprovingNodeId = item.ApprovingNodeId,
                    ApproverUserId = item.ApproverUserId,
                    ApproverFullName = item.ApproverFullName,
                    ApproverRole = item.ApproverRole,
                    ApproverDepartmentName = item.ApproverDepartmentName,
                    Decision = item.Decision,
                    Comment = item.Comment,
                    DecidedAtUtc = item.DecidedAtUtc
                })
                .ToList()
        };
    }
}


