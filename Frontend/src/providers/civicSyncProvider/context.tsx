import { createContext } from 'react';
import type { ChangeRequest, Citizen, DepartmentUser, LedgerEntry, NodeInfo, NodeOption, SyncInboxEntry, SyncOutboxEvent, SyncReceipt } from '../../api/types';

export const nodes: NodeOption[] = [
  { name: 'Home Affairs', departmentCode: 1, baseUrl: 'http://localhost:5076' },
  { name: 'SARS', departmentCode: 2, baseUrl: 'http://localhost:5077' },
  { name: 'Municipality', departmentCode: 3, baseUrl: 'http://localhost:5078' },
];

export const statusText: Record<number, string> = {
  1: 'Draft',
  2: 'Pending approval',
  3: 'Approved',
  4: 'Rejected',
  5: 'Committed',
  6: 'Syncing',
  7: 'Synced',
  8: 'Sync failed',
  9: 'Conflict',
};

export interface CitizenFormState {
  nationalIdNumber: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  phoneNumber: string;
}

export interface ChangeFormState {
  reason: string;
  newEmailAddress: string;
  newPhoneNumber: string;
}

export interface SubmitFieldChangeInput {
  fieldName: string;
  newValue: string;
  reason: string;
}

export interface CivicSyncStateContextValue {
  activeNode: NodeOption;
  nodeInfo: NodeInfo | null;
  citizens: Citizen[];
  users: DepartmentUser[];
  changeRequests: ChangeRequest[];
  requestNodeBaseUrls: Record<string, string>;
  ledger: LedgerEntry[];
  outbox: SyncOutboxEvent[];
  inbox: SyncInboxEntry[];
  receipts: SyncReceipt[];
  selectedCitizenId: string;
  selectedRequestId: string;
  citizenForm: CitizenFormState;
  changeForm: ChangeFormState;
  message: string;
  isLoading: boolean;
  isPending: boolean;
  isSuccess: boolean;
  isError: boolean;
  currentOperation: string;
  successMessage: string;
  errorMessage: string;
}

export interface CivicSyncActionContextValue {
  setActiveNode: (node: NodeOption) => void;
  setSelectedCitizenId: (id: string) => void;
  setSelectedRequestId: (id: string) => void;
  updateCitizenForm: (values: Partial<CitizenFormState>) => void;
  updateChangeForm: (values: Partial<ChangeFormState>) => void;
  refreshAll: () => Promise<void>;
  createCitizen: () => Promise<void>;
  submitChangeRequest: () => Promise<void>;
  submitFieldChangeRequest: (request: SubmitFieldChangeInput) => Promise<string>;
  requestApproval: (requestId?: string) => Promise<void>;
  approveRequest: (requestId?: string) => Promise<void>;
  commitRequest: (requestId?: string) => Promise<void>;
  publishOutbox: () => Promise<void>;
  applyInbox: () => Promise<void>;
}

export const initialCitizenForm: CitizenFormState = {
  nationalIdNumber: '',
  firstName: '',
  lastName: '',
  emailAddress: '',
  phoneNumber: '',
};

export const initialChangeForm: ChangeFormState = {
  reason: 'Update contact details',
  newEmailAddress: '',
  newPhoneNumber: '',
};

export const initialState: CivicSyncStateContextValue = {
  activeNode: nodes[0],
  nodeInfo: null,
  citizens: [],
  users: [],
  changeRequests: [],
  requestNodeBaseUrls: {},
  ledger: [],
  outbox: [],
  inbox: [],
  receipts: [],
  selectedCitizenId: '',
  selectedRequestId: '',
  citizenForm: initialCitizenForm,
  changeForm: initialChangeForm,
  message: 'Ready.',
  isLoading: false,
  isPending: false,
  isSuccess: false,
  isError: false,
  currentOperation: '',
  successMessage: '',
  errorMessage: '',
};

export const CivicSyncStateContext = createContext<CivicSyncStateContextValue | undefined>(undefined);
export const CivicSyncActionContext = createContext<CivicSyncActionContextValue | undefined>(undefined);

