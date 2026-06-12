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
    value: citizen?.dateOfBirth || fallback,
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA-owned civil registry field stored on the shared citizen profile.',
  },
  {
    key: 'PassportNumber',
    label: 'Passport Number',
    value: citizen?.passportNumber || fallback,
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA-owned travel identity field stored on the shared citizen profile.',
  },
  {
    key: 'BiometricReference',
    label: 'Biometric Reference',
    value: citizen?.biometricReference || fallback,
    category: 'Security',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA-owned biometric identity reference. Other departments should not view the raw biometric profile.',
  },
  {
    key: 'RelationshipStatus',
    label: 'Relationship Status',
    value: citizen?.relationshipStatus || fallback,
    category: 'Identity',
    ownerDepartmentCode: 1,
    accessDepartmentCodes: [1],
    approvalDepartmentCodes: [1],
    supportedByBackend: true,
    helper: 'DHA owns civil relationships such as spouse, parents, and children.',
  },
  {
    key: 'TaxNumber',
    label: 'Tax Number',
    value: citizen?.taxNumber || fallback,
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: true,
    helper: 'SARS-owned taxpayer identifier; not visible to DHA or municipality users.',
  },
  {
    key: 'EmploymentHistory',
    label: 'Employment History',
    value: citizen?.employmentHistory || fallback,
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: true,
    helper: 'SARS financial profile data from employers and third-party reporting.',
  },
  {
    key: 'IncomeAndInvestmentProfile',
    label: 'Income and Investments',
    value: citizen?.incomeAndInvestmentProfile || fallback,
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: true,
    helper: 'SARS-only financial data. DHA should not access this profile.',
  },
  {
    key: 'BankingAndAssets',
    label: 'Banking and Assets',
    value: citizen?.bankingAndAssets || fallback,
    category: 'Tax',
    ownerDepartmentCode: 2,
    accessDepartmentCodes: [2],
    approvalDepartmentCodes: [2],
    supportedByBackend: true,
    helper: 'SARS-only asset and transaction intelligence from third-party data providers.',
  },
  {
    key: 'ResidentialAddress',
    label: 'Residential Address',
    value: citizen?.residentialAddress || fallback,
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [2, 3],
    approvalDepartmentCodes: [3],
    supportedByBackend: true,
    helper: 'Municipality owns service address data; SARS may hold a tax address view.',
  },
  {
    key: 'RatesAccount',
    label: 'Rates Account',
    value: citizen?.ratesAccount || fallback,
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [3],
    approvalDepartmentCodes: [3],
    supportedByBackend: true,
    helper: 'Municipality-owned service and billing account.',
  },
  {
    key: 'MunicipalServiceStatus',
    label: 'Service Status',
    value: citizen?.municipalServiceStatus || fallback,
    category: 'Municipal',
    ownerDepartmentCode: 3,
    accessDepartmentCodes: [3],
    approvalDepartmentCodes: [3],
    supportedByBackend: true,
    helper: 'Municipality-owned service state for local records.',
  },
];

export const normalizeFieldName = (value: string) => value.replace(/\s/g, '').toLowerCase();

export const getCitizenFieldLabel = (fieldName: string) => {
  const normalizedFieldName = normalizeFieldName(fieldName);

  if (normalizedFieldName === 'contactdetails') {
    return 'Contact Details';
  }

  return buildCitizenFieldPolicies().find((field) => {
    const fieldKey = normalizeFieldName(field.key);
    const fieldLabel = normalizeFieldName(field.label);

    return fieldKey === normalizedFieldName || fieldLabel === normalizedFieldName;
  })?.label ?? fieldName;
};

export const formatCitizenFieldValue = (fieldName: string, value: string) => {
  if (!value) {
    return 'No value recorded';
  }

  if (normalizeFieldName(fieldName) !== 'contactdetails') {
    return value;
  }

  const [emailAddress, phoneNumber] = value.split('|');
  const parts = [
    emailAddress ? `Email: ${emailAddress}` : '',
    phoneNumber ? `Phone: ${phoneNumber}` : '',
  ].filter(Boolean);

  return parts.length > 0 ? parts.join(' • ') : value;
};

export const formatCitizenFieldChange = (fieldName: string, newValue: string) =>
  `${getCitizenFieldLabel(fieldName)} update requested: ${formatCitizenFieldValue(fieldName, newValue)}`;
