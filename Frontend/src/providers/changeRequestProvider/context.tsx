import { createContext } from 'react';
import type { ChangeRequest } from '../../api/types';
import type { ChangeFormState, SubmitFieldChangeInput } from '../civicSyncProvider/context';

export interface ChangeRequestStateContextValue {
  changeRequests: ChangeRequest[];
  selectedRequest?: ChangeRequest;
  selectedRequestId: string;
  requestNodeBaseUrls: Record<string, string>;
  changeForm: ChangeFormState;
}

export interface ChangeRequestActionContextValue {
  setSelectedRequestId: (id: string) => void;
  updateChangeForm: (values: Partial<ChangeFormState>) => void;
  submitChangeRequest: () => Promise<void>;
  submitFieldChangeRequest: (request: SubmitFieldChangeInput) => Promise<string>;
  requestApproval: (requestId?: string) => Promise<void>;
  approveRequest: (requestId?: string) => Promise<void>;
  commitRequest: (requestId?: string) => Promise<void>;
}

export const ChangeRequestStateContext = createContext<ChangeRequestStateContextValue | undefined>(undefined);
export const ChangeRequestActionContext = createContext<ChangeRequestActionContextValue | undefined>(undefined);
