import type { CivicSyncStateContextValue } from './context';

export enum CivicSyncActionEnums {
  setState = 'SET_STATE',
  setMessage = 'SET_MESSAGE',
  setOperationPending = 'SET_OPERATION_PENDING',
  setOperationSuccess = 'SET_OPERATION_SUCCESS',
  setOperationError = 'SET_OPERATION_ERROR',
}

export type CivicSyncAction =
  | { type: CivicSyncActionEnums.setState; payload: Partial<CivicSyncStateContextValue> }
  | { type: CivicSyncActionEnums.setMessage; payload: string }
  | { type: CivicSyncActionEnums.setOperationPending; payload: string }
  | { type: CivicSyncActionEnums.setOperationSuccess; payload: string }
  | { type: CivicSyncActionEnums.setOperationError; payload: string };

export const setCivicSyncState = (payload: Partial<CivicSyncStateContextValue>): CivicSyncAction => ({
  type: CivicSyncActionEnums.setState,
  payload,
});

export const setCivicSyncMessage = (payload: string): CivicSyncAction => ({
  type: CivicSyncActionEnums.setMessage,
  payload,
});

export const setOperationPending = (payload: string): CivicSyncAction => ({
  type: CivicSyncActionEnums.setOperationPending,
  payload,
});

export const setOperationSuccess = (payload: string): CivicSyncAction => ({
  type: CivicSyncActionEnums.setOperationSuccess,
  payload,
});

export const setOperationError = (payload: string): CivicSyncAction => ({
  type: CivicSyncActionEnums.setOperationError,
  payload,
});
