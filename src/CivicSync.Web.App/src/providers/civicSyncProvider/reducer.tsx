import { CivicSyncActionEnums, type CivicSyncAction } from './actions';
import type { CivicSyncStateContextValue } from './context';

export const civicSyncReducer = (state: CivicSyncStateContextValue, action: CivicSyncAction): CivicSyncStateContextValue => {
  switch (action.type) {
    case CivicSyncActionEnums.setState:
      return { ...state, ...action.payload };
    case CivicSyncActionEnums.setLoading:
      return { ...state, isLoading: action.payload };
    case CivicSyncActionEnums.setMessage:
      return { ...state, message: action.payload };
    default:
      return state;
  }
};
