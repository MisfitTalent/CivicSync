import type { SyncStateContextValue } from './context';

export enum SyncActionEnums {
  setState = 'SET_STATE',
}

export type SyncAction = {
  type: SyncActionEnums.setState;
  payload: Partial<SyncStateContextValue>;
};

export const setSyncState = (payload: Partial<SyncStateContextValue>): SyncAction => ({
  type: SyncActionEnums.setState,
  payload,
});
