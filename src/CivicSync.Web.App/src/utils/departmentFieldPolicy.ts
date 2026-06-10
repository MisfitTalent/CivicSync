import type { Citizen, DepartmentCode } from '../api/types';

export interface CitizenFieldPolicy {
  key: string;
  label: string;
  value: string;
  category: 'Identity' | 'Contact' | 'Tax' | 'Municipal' | 'Security';
  ownerDepartmentCode: DepartmentCode;
  accessDepartmentCodes: DepartmentCode[];
  approvalDepartmentCodes: DepartmentCode[];
  supportedByBackend: boolean;
  helper: string;
}

const fallback = 'Unavailable';

export const departmentShortName: Record<DepartmentCode, string> = {
  1: 'HA',
  2: 'SARS',
  3: 'MUN',
  4: 'HEALTH',
  5: 'SAFETY',
};

export const departmentDisplayName: Record<DepartmentCode, string> = {
  1: 'Home Affairs',
  2: 'SARS',
  3: 'Municipality',
  4: 'Health',
  5: 'Safety',
};

export const buildCitizenFieldPolicies = (citizen?: Citizen): CitizenFieldPolicy[] => [
  {
    key: 'FullName',
    label: 'Full Name',
    value: citizen?.displayName || 'No citizen selected',
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1, 2, 3],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA-owned legal identity field used by SARS and municipalities for verification.',
  },
  {
    key: 'NationalIdNumber',
    label: 'National ID',
    value: citizen?.nationalIdNumber || fallback,
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1, 2],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA-owned identity number; SARS may verify tax profiles against it.',
  },
  {
    key: 'EmailAddress',
    label: 'Email Address',
    value: citizen?.emailAddress || fallback,
    category: 'Contact',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1, 2, 3],
    approvalDepartmentCodes: [1, 2, 3],
    supportedByBackend: true,
    helper: 'Shared contact detail stored on the citizen record.',
  },
  {
    key: 'PhoneNumber',
    label: 'Phone Number',
    value: citizen?.phoneNumber || fallback,
    category: 'Contact',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1, 2, 3],
    approvalDepartmentCodes: [1, 2, 3],
    supportedByBackend: true,
    helper: 'Shared contact detail stored on the citizen record.',
  },
  {
    key: 'DateOfBirth',
    label: 'Date of Birth',
    value: '28 August 2000',
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: false,
    helper: 'DHA-owned civil registry field; not implemented in the backend shared citizen table yet.',
  },
  {
    key: 'PassportNumber',
    label: 'Passport Number',
    value: 'M12345678',
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: false,
    helper: 'DHA-owned travel identity field; backend support is still future scope.',
  },
  {
    key: 'BiometricReference',
    label: 'Biometric Reference',
    value: 'Fingerprint and facial scan enrolled',
    category: 'Security',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: false,
    helper: 'DHA-owned biometric identity data. Other departments should not view the raw biometric profile.',
  },
  {
    key: 'RelationshipStatus',
    label: 'Relationship Status',
    value: 'Civil registry relationships',
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: false,
    helper: 'DHA owns civil relationships such as spouse, parents, and children.',
  },
  {
    key: 'TaxNumber',
    label: 'Tax Number',
    value: '9876543210',
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: false,
    helper: 'SARS-owned taxpayer identifier; not visible to DHA or municipality users.',
  },
  {
    key: 'EmploymentHistory',
    label: 'Employment History',
    value: 'Employer payroll and IRP5 history',
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: false,
    helper: 'SARS financial profile data from employers and third-party reporting.',
  },
  {
    key: 'IncomeAndInvestmentProfile',
    label: 'Income and Investments',
    value: 'Income, interest, investment returns, and offshore disclosures',
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: false,
    helper: 'SARS-only financial data. DHA should not access this profile.',
  },
  {
    key: 'BankingAndAssets',
    label: 'Banking and Assets',
    value: 'Bank, asset, property, and rental-income profile',
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: false,
    helper: 'SARS-only asset and transaction intelligence from third-party data providers.',
  },
  {
    key: 'ResidentialAddress',
    label: 'Residential Address',
    value: '14 Ubuntu Street, Soweto, 1804',
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [2, 3],
    approvalDepartmentCodes: [3],
    supportedByBackend: false,
    helper: 'Municipality owns service address data; SARS may hold a tax address view.',
  },
  {
    key: 'RatesAccount',
    label: 'Rates Account',
    value: 'MUN-2024-88821',
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [3],
    approvalDepartmentCodes: [3],
    supportedByBackend: false,
    helper: 'Municipality-owned service and billing account.',
  },
  {
    key: 'MunicipalServiceStatus',
    label: 'Service Status',
    value: 'Active municipal services',
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [3],
    approvalDepartmentCodes: [3],
    supportedByBackend: false,
    helper: 'Municipality-owned service state for local records.',
  },
];
