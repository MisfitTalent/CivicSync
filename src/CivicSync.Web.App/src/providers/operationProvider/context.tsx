import { createContext } from 'react';

export interface OperationStateContextValue {
  message: string;
  isLoading: boolean;
  isPending: boolean;
  isSuccess: boolean;
  isError: boolean;
  currentOperation: string;
  successMessage: string;
  errorMessage: string;
}

export const OperationStateContext = createContext<OperationStateContextValue | undefined>(undefined);
