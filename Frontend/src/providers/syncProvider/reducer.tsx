import { SyncActionEnums, type SyncAction } from './actions';
import type { SyncStateContextValue } from './context';

export const syncReducer = (
  state: SyncStateContextValue,
  action: SyncAction,
): SyncStateContextValue => {
  switch (action.type) {
    case SyncActionEnums.setState:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};
