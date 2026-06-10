import { useCallback, useContext, useEffect, useMemo, useReducer } from 'react';
import { CivicSyncClient } from '../../api/civicsyncClient';
import { getErrorMessage } from '../../utils/axiosInstance';
import { setCivicSyncState, setOperationError, setOperationPending, setOperationSuccess } from './actions';
import {
  CivicSyncActionContext,
  CivicSyncStateContext,
  initialChangeForm,
  initialCitizenForm,
  initialState,
  type ChangeFormState,
  type CitizenFormState,
} from './context';
import type { NodeOption } from '../../api/types';
import { civicSyncReducer } from './reducer';

export const CivicSyncProvider = ({ children }: { children: React.ReactNode }) => {
  const [state, dispatch] = useReducer(civicSyncReducer, initialState);
  const client = useMemo(() => new CivicSyncClient(state.activeNode.baseUrl), [state.activeNode.baseUrl]);

  const refreshAll = useCallback(async (showMessage = true) => {
    if (showMessage) {
      dispatch(setOperationPending('Refresh node data'));
    }

    try {
      const [nodeInfo, citizens, users, changeRequests, ledger, outbox, inbox, receipts] = await Promise.all([
        client.getNodeInfo(),
        client.getCitizens(),
        client.getDepartmentUsers(),
        client.getChangeRequests(),
        client.getLedger(),
        client.getOutbox(),
        client.getInbox(),
        client.getReceipts(),
      ]);

      dispatch(setCivicSyncState({
          nodeInfo,
          citizens,
          users,
          changeRequests,
          ledger,
          outbox,
          inbox,
          receipts,
          selectedCitizenId: state.selectedCitizenId || citizens[0]?.id || '',
          selectedRequestId: state.selectedRequestId || changeRequests[0]?.id || '',
          message: showMessage ? `Connected to ${state.activeNode.name}.` : state.message,
        }));

      if (showMessage) {
        dispatch(setOperationSuccess(`Connected to ${state.activeNode.name}.`));
      }
    } catch (error) {
      const errorMessage = getErrorMessage(error);
      if (showMessage) {
        dispatch(setOperationError(errorMessage));
        return;
      }

      throw error;
    }
  }, [client, state.activeNode.name, state.message, state.selectedCitizenId, state.selectedRequestId]);

  useEffect(() => {
    refreshAll();
  }, [state.activeNode.baseUrl]);

  const runAction = async (label: string, action: () => Promise<void>) => {
    dispatch(setOperationPending(label));
    try {
      await action();
      await refreshAll(false);
      dispatch(setOperationSuccess(`${label} completed.`));
    } catch (error) {
      dispatch(setOperationError(getErrorMessage(error)));
    }
  };

  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const firstApprover = state.users[0];

  const actions = {
    setActiveNode: (node: NodeOption) => dispatch(setCivicSyncState({ activeNode: node, selectedCitizenId: '', selectedRequestId: '' })),
    setSelectedCitizenId: (id: string) => dispatch(setCivicSyncState({ selectedCitizenId: id })),
    setSelectedRequestId: (id: string) => dispatch(setCivicSyncState({ selectedRequestId: id })),
    updateCitizenForm: (values: Partial<CitizenFormState>) => dispatch(setCivicSyncState({ citizenForm: { ...state.citizenForm, ...values } })),
    updateChangeForm: (values: Partial<ChangeFormState>) => dispatch(setCivicSyncState({ changeForm: { ...state.changeForm, ...values } })),
    refreshAll: () => refreshAll(),
    createCitizen: () => runAction('Create citizen', async () => {
      const created = await client.createCitizen(state.citizenForm);
      dispatch(setCivicSyncState({ selectedCitizenId: created.id, citizenForm: initialCitizenForm }));
    }),
    submitChangeRequest: () => runAction('Submit change request', async () => {
      if (!selectedCitizen) {
        throw new Error('Select a citizen first.');
      }

      const created = await client.submitChangeRequest({
        citizenId: selectedCitizen.id,
        reason: state.changeForm.reason,
        fieldChanges: [
          {
            fieldName: 'ContactDetails',
            newValue: `${state.changeForm.newEmailAddress}|${state.changeForm.newPhoneNumber}`,
          },
        ],
      });

      dispatch(setCivicSyncState({ selectedRequestId: created.id, changeForm: initialChangeForm }));
    }),
    requestApproval: () => runAction('Request approval', async () => {
      if (!selectedRequest || !firstApprover) {
        throw new Error('Select a request and make sure this node has a department user.');
      }

      await client.requestApproval(selectedRequest.id, firstApprover.departmentNodeId, firstApprover.id);
    }),
    approveRequest: () => runAction('Approve request', async () => {
      if (!selectedRequest || !firstApprover) {
        throw new Error('Select a request and make sure this node has a department user.');
      }

      await client.recordDecision(selectedRequest.id, firstApprover.departmentNodeId, firstApprover.id, 'Approved from CivicSync frontend');
    }),
    commitRequest: () => runAction('Commit request', async () => {
      if (!selectedRequest) {
        throw new Error('Select a request first.');
      }

      await client.commitChangeRequest(selectedRequest.id);
    }),
    publishOutbox: () => runAction('Publish outbox', async () => {
      await client.publishOutbox();
    }),
    applyInbox: () => runAction('Apply inbox', async () => {
      await client.applyInbox();
    }),
  };

  return (
    <CivicSyncStateContext.Provider value={state}>
      <CivicSyncActionContext.Provider value={actions}>{children}</CivicSyncActionContext.Provider>
    </CivicSyncStateContext.Provider>
  );
};

export const useCivicSyncState = () => {
  const context = useContext(CivicSyncStateContext);
  if (!context) {
    throw new Error('useCivicSyncState must be used within CivicSyncProvider');
  }
  return context;
};

export const useCivicSyncActions = () => {
  const context = useContext(CivicSyncActionContext);
  if (!context) {
    throw new Error('useCivicSyncActions must be used within CivicSyncProvider');
  }
  return context;
};
