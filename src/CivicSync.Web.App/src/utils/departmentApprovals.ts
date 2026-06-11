import type { ChangeRequest, DepartmentApproval, DepartmentCode } from '../api/types';
import { departmentDisplayName, departmentShortName } from './departmentFieldPolicy';

const departmentApprovalNames: Record<DepartmentCode, string[]> = {
  1: ['1', 'HA', 'HomeAffairs', 'Home Affairs'],
  2: ['2', 'SARS', 'Sars'],
  3: ['3', 'MUN', 'Municipality'],
  4: ['4', 'HEALTH', 'Health'],
  5: ['5', 'SAFETY', 'Safety'],
};

export const normalizeDepartmentName = (value = '') => value.toLowerCase().replace(/[^a-z0-9]/g, '');

export const findDepartmentApproval = (
  request: ChangeRequest,
  departmentCode: DepartmentCode,
): DepartmentApproval | undefined => {
  const allowedNames = [
    ...departmentApprovalNames[departmentCode],
    departmentDisplayName[departmentCode],
    departmentShortName[departmentCode],
    String(departmentCode),
  ].map(normalizeDepartmentName);

  return request.approvals.find((approval) =>
    allowedNames.includes(normalizeDepartmentName(approval.approverDepartmentName))
  );
};

export const requestNeedsDepartmentReview = (request: ChangeRequest, departmentCode: DepartmentCode) => {
  const approval = findDepartmentApproval(request, departmentCode);
  const requestCanStillBeReviewed = request.status !== 4 && request.status !== 5;

  return Boolean(approval && approval.decision === 1 && requestCanStillBeReviewed);
};

export const requestApprovedByDepartment = (request: ChangeRequest, departmentCode: DepartmentCode) =>
  findDepartmentApproval(request, departmentCode)?.decision === 2;
