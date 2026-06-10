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
  type SubmitFieldChangeInput,
} from './context';
import type { ChangeRequest, Citizen, DepartmentUser, LedgerEntry, NodeInfo, NodeOption, SyncInboxEntry, SyncOutboxEvent, SyncReceipt } from '../../api/types';
import { civicSyncReducer } from './reducer';

const resolveSettledValue = <T,>(result: PromiseSettledResult<unknown>, fallback: T) => {
  return result.status === 'fulfilled' ? result.value as T : fallback;
};

const getRejectedMessages = (results: PromiseSettledResult<unknown>[]) => {
  return results
    .filter((result): result is PromiseRejectedResult => result.status === 'rejected')
    .map((result) => getErrorMessage(result.reason));
};

export const CivicSyncProvider = ({ children }: { children: React.ReactNode }) => {
  const [state, dispatch] = useReducer(civicSyncReducer, initialState);
  const client = useMemo(() => new CivicSyncClient(state.activeNode.baseUrl), [state.activeNode.baseUrl]);

  const refreshAll = useCallback(async (showMessage = true) => {
    if (showMessage) {
      dispatch(setOperationPending('Refresh node data'));
    }

    try {
      const results = await Promise.allSettled([
        client.getNodeInfo(),
        client.getCitizens(),
        client.getDepartmentUsers(),
        client.getChangeRequests(),
        client.getLedger(),
        client.getOutbox(),
        client.getInbox(),
        client.getReceipts(),
      ]);

      const nodeInfo = resolveSettledValue<NodeInfo | null>(results[0], state.nodeInfo);
      const citizens = resolveSettledValue<Citizen[]>(results[1], state.citizens);
      const users = resolveSettledValue<DepartmentUser[]>(results[2], state.users);
      const changeRequests = resolveSettledValue<ChangeRequest[]>(results[3], state.changeRequests);
      const ledger = resolveSettledValue<LedgerEntry[]>(results[4], state.ledger);
      const outbox = resolveSettledValue<SyncOutboxEvent[]>(results[5], state.outbox);
      const inbox = resolveSettledValue<SyncInboxEntry[]>(results[6], state.inbox);
      const receipts = resolveSettledValue<SyncReceipt[]>(results[7], state.receipts);
      const rejectedMessages = getRejectedMessages(results);
      const hasConnectionFailure = rejectedMessages.length > 0;
      const statusMessage = hasConnectionFailure
        ? `${state.activeNode.name} partially loaded. ${rejectedMessages[0]}`
        : `Connected to ${state.activeNode.name}.`;

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
          message: showMessage ? statusMessage : state.message,
        }));

      if (showMessage) {
        if (hasConnectionFailure) {
          dispatch(setOperationError(statusMessage));
          return;
        }

        dispatch(setOperationSuccess(statusMessage));
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
  const buildSharedFieldChange = (request: SubmitFieldChangeInput) => {
    if (!selectedCitizen) {
      throw new Error('Select a citizen first.');
    }

    const fieldName = request.fieldName.trim();
    const newValue = request.newValue.trim();

    if (!request.reason.trim()) {
      throw new Error('Provide a reason for the change request.');
    }

    if (!newValue) {
      throw new Error('Provide the new field value.');
    }

    switch (fieldName) {
      case 'FullName':
      case 'NationalIdNumber':
        return { fieldName, newValue };
      case 'EmailAddress':
        return { fieldName: 'ContactDetails', newValue: `${newValue}|${selectedCitizen.phoneNumber}` };
      case 'PhoneNumber':
        return { fieldName: 'ContactDetails', newValue: `${selectedCitizen.emailAddress}|${newValue}` };
      default:
        throw new Error(`${fieldName} is not supported by the backend shared citizen record yet.`);
    }
  };

  const getTargetRequest = (requestId?: string) => {
    return state.changeRequests.find((request) => request.id === (requestId || state.selectedRequestId));
  };

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

      if (!state.changeForm.reason.trim()) {
        throw new Error('Provide a reason for the change request.');
      }

      if (!state.changeForm.newEmailAddress.trim() || !state.changeForm.newPhoneNumber.trim()) {
        throw new Error('Provide both a new email address and phone number.');
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
    submitFieldChangeRequest: async (request: SubmitFieldChangeInput) => {
      dispatch(setOperationPending('Submit field change request'));

      try {
        if (!selectedCitizen) {
          throw new Error('Select a citizen first.');
        }

        const fieldChange = buildSharedFieldChange(request);
        const created = await client.submitChangeRequest({
          citizenId: selectedCitizen.id,
          reason: request.reason.trim(),
          fieldChanges: [fieldChange],
        });

        dispatch(setCivicSyncState({ selectedRequestId: created.id }));
        await refreshAll(false);
        dispatch(setOperationSuccess('Submit field change request completed.'));
        return created.id;
      } catch (error) {
        dispatch(setOperationError(getErrorMessage(error)));
        throw error;
      }
    },
    requestApproval: (requestId?: string) => runAction('Request approval', async () => {
      const targetRequest = getTargetRequest(requestId);

      if (!targetRequest || !firstApprover) {
        throw new Error('Select a request and make sure this node has a department user.');
      }

      const existingApproval = targetRequest.approvals.find((approval) => approval.approvingNodeId === firstApprover.departmentNodeId);

      if (existingApproval) {
        throw new Error('This node has already been asked to approve the selected request.');
      }

      const updated = await client.requestApproval(targetRequest.id, firstApprover.departmentNodeId, firstApprover.id);
      dispatch(setCivicSyncState({ selectedRequestId: updated.id }));
    }),
    approveRequest: (requestId?: string) => runAction('Approve request', async () => {
      const targetRequest = getTargetRequest(requestId);

      if (!targetRequest || !firstApprover) {
        throw new Error('Select a request and make sure this node has a department user.');
      }

      const existingApproval = targetRequest.approvals.find((approval) => approval.approvingNodeId === firstApprover.departmentNodeId);
      const requestToApprove = existingApproval
        ? targetRequest
        : await client.requestApproval(targetRequest.id, firstApprover.departmentNodeId, firstApprover.id);

      await client.recordDecision(requestToApprove.id, firstApprover.departmentNodeId, firstApprover.id, 'Approved from CivicSync frontend');
      dispatch(setCivicSyncState({ selectedRequestId: requestToApprove.id }));
    }),
    commitRequest: (requestId?: string) => runAction('Commit request', async () => {
      const targetRequest = getTargetRequest(requestId);

      if (!targetRequest) {
        throw new Error('Select a request first.');
      }

      if (targetRequest.status !== 3) {
        throw new Error('Only approved change requests can be committed.');
      }

      await client.commitChangeRequest(targetRequest.id);
      dispatch(setCivicSyncState({ selectedRequestId: targetRequest.id }));
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
