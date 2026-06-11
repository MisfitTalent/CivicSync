export type DepartmentCode = 1 | 2 | 3 | 4 | 5;

export interface NodeOption {
  name: string;
  departmentCode: DepartmentCode;
  baseUrl: string;
}

export interface NodeInfo {
  departmentCode: DepartmentCode;
  apiBaseUrl: string;
  peers: PeerNode[];
}

export interface PeerNode {
  departmentCode: DepartmentCode;
  apiBaseUrl: string;
}

export interface Citizen {
  id: string;
  departmentNodeId: string;
  nationalIdNumber: string;
  firstName: string;
  lastName: string;
  displayName: string;
  emailAddress: string;
  phoneNumber: string;
  dateOfBirth: string;
  passportNumber: string;
  biometricReference: string;
  relationshipStatus: string;
  taxNumber: string;
  employmentHistory: string;
  incomeAndInvestmentProfile: string;
  bankingAndAssets: string;
  residentialAddress: string;
  ratesAccount: string;
  municipalServiceStatus: string;
  status: number;
  recordVersion: number;
  createdAtUtc: string;
}

export interface DepartmentUser {
  id: string;
  departmentNodeId: string;
  fullName: string;
  role: string;
  emailAddress: string;
  isActive: boolean;
}

export interface FieldChange {
  id: string;
  fieldName: string;
  oldValue: string;
  newValue: string;
}

export interface DepartmentApproval {
  id: string;
  approvingNodeId: string;
  approverUserId: string;
  approverFullName: string;
  approverRole: string;
  approverDepartmentName: string;
  decision: number;
  comment?: string;
  decidedAtUtc?: string;
}

export interface ChangeRequest {
  id: string;
  requestedAtNodeId: string;
  citizenId: string;
  reason: string;
  status: number;
  expectedCitizenVersion: number;
  committedCitizenVersion?: number;
  fieldChanges: FieldChange[];
  approvals: DepartmentApproval[];
  createdAtUtc: string;
}

export interface LedgerEntry {
  id: string;
  originatingNodeId: string;
  changeRequestId: string;
  sequenceNumber: number;
  eventType: number;
  payloadProofHash: string;
  previousProofHash: string;
  currentProofHash: string;
  createdAtUtc: string;
}

export interface SyncOutboxEvent {
  id: string;
  departmentNodeId: string;
  ledgerEntryId: string;
  status: number;
  retryCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface SyncInboxEntry {
  id: string;
  departmentNodeId: string;
  ledgerEntryId: string;
  receivedFromNodeId: string;
  citizenNationalIdNumber: string;
  fieldChangesJson: string;
  status: number;
  appliedAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface SyncReceipt {
  id: string;
  syncOutboxEventId: string;
  targetNodeId: string;
  result: number;
  receivedAtUtc: string;
}

export interface CreateCitizenRequest {
  nationalIdNumber: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  phoneNumber: string;
  dateOfBirth?: string;
  passportNumber?: string;
  biometricReference?: string;
  relationshipStatus?: string;
  taxNumber?: string;
  employmentHistory?: string;
  incomeAndInvestmentProfile?: string;
  bankingAndAssets?: string;
  residentialAddress?: string;
  ratesAccount?: string;
  municipalServiceStatus?: string;
}

export interface SubmitChangeRequest {
  citizenId: string;
  reason: string;
  fieldChanges: Array<{
    fieldName: string;
    newValue: string;
  }>;
}

export interface CommitChangeResponse {
  changeRequestId: string;
  status: string;
  ledgerEntry: LedgerEntry;
}

export interface PublishOutboxResponse {
  processedOutboxEvents: number;
  skippedOutboxEvents: number;
  successfulPeerDeliveries: number;
  failedPeerDeliveries: number;
  peerResults: Array<{
    departmentCode: DepartmentCode;
    apiBaseUrl: string;
    result: number;
    retryCount: number;
    message: string;
  }>;
}

export interface ApplyInboxResponse {
  appliedInboxEntries: number;
  failedInboxEntries: number;
}