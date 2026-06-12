import { CivicSyncActionEnums, type CivicSyncAction } from './actions';
import type { CivicSyncStateContextValue } from './context';

export const civicSyncReducer = (state: CivicSyncStateContextValue, action: CivicSyncAction): CivicSyncStateContextValue => {
  switch (action.type) {
    case CivicSyncActionEnums.setState:
      return { ...state, ...action.payload };
    case CivicSyncActionEnums.setMessage:
      return { ...state, message: action.payload };
    case CivicSyncActionEnums.setOperationPending:
      return {
        ...state,
        isLoading: true,
        isPending: true,
        isSuccess: false,
        isError: false,
        currentOperation: action.payload,
        message: `${action.payload}...`,
        successMessage: '',
        errorMessage: '',
      };
    case CivicSyncActionEnums.setOperationSuccess:
      return {
        ...state,
        isLoading: false,
        isPending: false,
        isSuccess: true,
        isError: false,
        message: action.payload,
        successMessage: action.payload,
        errorMessage: '',
      };
    case CivicSyncActionEnums.setOperationError:
      return {
        ...state,
        isLoading: false,
        isPending: false,
        isSuccess: false,
        isError: true,
        message: action.payload,
        successMessage: '',
        errorMessage: action.payload,
      };
    default:
      return state;
  }
};
