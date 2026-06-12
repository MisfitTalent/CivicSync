import { useContext, useMemo, type ReactNode } from 'react';
import { useCivicSyncActions, useCivicSyncState } from '../civicSyncProvider';
import { ChangeRequestActionContext, ChangeRequestStateContext } from './context';

export const ChangeRequestProvider = ({ children }: { children: ReactNode }) => {
  const { changeRequests, selectedRequestId, requestNodeBaseUrls, changeForm } = useCivicSyncState();
  const {
    setSelectedRequestId,
    updateChangeForm,
    submitChangeRequest,
    submitFieldChangeRequest,
    requestApproval,
    approveRequest,
    commitRequest,
  } = useCivicSyncActions();
  const selectedRequest = useMemo(
    () => changeRequests.find((request) => request.id === selectedRequestId),
    [changeRequests, selectedRequestId],
  );

  const state = useMemo(
    () => ({ changeRequests, selectedRequest, selectedRequestId, requestNodeBaseUrls, changeForm }),
    [changeRequests, selectedRequest, selectedRequestId, requestNodeBaseUrls, changeForm],
  );
  const actions = useMemo(
    () => ({
      setSelectedRequestId,
      updateChangeForm,
      submitChangeRequest,
      submitFieldChangeRequest,
      requestApproval,
      approveRequest,
      commitRequest,
    }),
    [
      setSelectedRequestId,
      updateChangeForm,
      submitChangeRequest,
      submitFieldChangeRequest,
      requestApproval,
      approveRequest,
      commitRequest,
    ],
  );

  return (
    <ChangeRequestStateContext.Provider value={state}>
      <ChangeRequestActionContext.Provider value={actions}>{children}</ChangeRequestActionContext.Provider>
    </ChangeRequestStateContext.Provider>
  );
};

export const useChangeRequestState = () => {
  const context = useContext(ChangeRequestStateContext);
  if (!context) {
    throw new Error('useChangeRequestState must be used within ChangeRequestProvider');
  }
  return context;
};

export const useChangeRequestActions = () => {
  const context = useContext(ChangeRequestActionContext);
  if (!context) {
    throw new Error('useChangeRequestActions must be used within ChangeRequestProvider');
  }
  return context;
};
