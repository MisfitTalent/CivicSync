import { useContext, useMemo, type ReactNode } from 'react';
import { useCivicSyncState } from '../civicSyncProvider';
import { OperationStateContext } from './context';

export const OperationProvider = ({ children }: { children: ReactNode }) => {
  const {
    message,
    isLoading,
    isPending,
    isSuccess,
    isError,
    currentOperation,
    successMessage,
    errorMessage,
  } = useCivicSyncState();

  const state = useMemo(() => ({
    message,
    isLoading,
    isPending,
    isSuccess,
    isError,
    currentOperation,
    successMessage,
    errorMessage,
  }), [message, isLoading, isPending, isSuccess, isError, currentOperation, successMessage, errorMessage]);

  return <OperationStateContext.Provider value={state}>{children}</OperationStateContext.Provider>;
};

export const useOperationState = () => {
  const context = useContext(OperationStateContext);
  if (!context) {
    throw new Error('useOperationState must be used within OperationProvider');
  }
  return context;
};
