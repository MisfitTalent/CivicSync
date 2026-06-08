import type { CivicSyncStateContextValue } from './context';

export enum CivicSyncActionEnums {
  setState = 'SET_STATE',
  setLoading = 'SET_LOADING',
  setMessage = 'SET_MESSAGE',
}

export type CivicSyncAction =
  | { type: CivicSyncActionEnums.setState; payload: Partial<CivicSyncStateContextValue> }
  | { type: CivicSyncActionEnums.setLoading; payload: boolean }
  | { type: CivicSyncActionEnums.setMessage; payload: string };

export const setCivicSyncState = (payload: Partial<CivicSyncStateContextValue>): CivicSyncAction => ({
  type: CivicSyncActionEnums.setState,
  payload,
});

export const setCivicSyncLoading = (payload: boolean): CivicSyncAction => ({
  type: CivicSyncActionEnums.setLoading,
  payload,
});

export const setCivicSyncMessage = (payload: string): CivicSyncAction => ({
  type: CivicSyncActionEnums.setMessage,
  payload,
});
