import { useCallback, useContext, useEffect, useMemo, useReducer, useRef } from 'react';
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
  nodes,
  type SubmitFieldChangeInput,
} from './context';
import type { ChangeRequest, Citizen, DepartmentUser, LedgerEntry, NodeInfo, NodeOption, SyncInboxEntry, SyncOutboxEvent, SyncReceipt } from '../../api/types';
import { civicSyncReducer } from './reducer';
import { findDepartmentApproval } from '../../utils/departmentApprovals';

const resolveSettledValue = <T,>(result: PromiseSettledResult<unknown>, fallback: T) => {
  return result.status === 'fulfilled' ? result.value as T : fallback;
};

const getRejectedMessages = (results: PromiseSettledResult<unknown>[]) => {
  return results
    .filter((result): result is PromiseRejectedResult => result.status === 'rejected')
    .map((result) => getErrorMessage(result.reason));
};

const loadOptionalDepartmentUsers = async (client: CivicSyncClient) => {
  try {
    return await client.getDepartmentUsers();
  } catch (error) {
    if (getErrorMessage(error).includes('404')) {
      return [] as DepartmentUser[];
    }

    throw error;
  }
};

const runSilentSyncMaintenance = async (client: CivicSyncClient) => {
  await Promise.allSettled([
    client.publishOutbox(),
    client.applyInbox(),
  ]);
};

interface NodeChangeRequestResult {
  node: NodeOption;
  requests: ChangeRequest[];
}

const getApprovalMergeKey = (approval: ChangeRequest['approvals'][number]) => {
  return approval.id || approval.approvingNodeId || approval.approverDepartmentName;
};

const getEvidenceMergeKey = (evidenceFile: ChangeRequest['evidenceFiles'][number]) => {
  return evidenceFile.id || `${evidenceFile.fileName}:${evidenceFile.contentHash}`;
};

const getRequestCompletenessScore = (request: ChangeRequest) => {
  return request.fieldChanges.length * 5 + (request.evidenceFiles?.length ?? 0) * 5 + request.approvals.length * 10 + request.status;
};

const mergeChangeRequest = (existing: ChangeRequest, incoming: ChangeRequest) => {
  const useIncomingBase = getRequestCompletenessScore(incoming) >= getRequestCompletenessScore(existing);
  const baseRequest = useIncomingBase ? incoming : existing;
  const secondaryRequest = useIncomingBase ? existing : incoming;
  const approvalsByKey = new Map<string, ChangeRequest['approvals'][number]>();
  const evidenceByKey = new Map<string, ChangeRequest['evidenceFiles'][number]>();

  [...secondaryRequest.approvals, ...baseRequest.approvals].forEach((approval) => {
    approvalsByKey.set(getApprovalMergeKey(approval), approval);
  });

  [...(secondaryRequest.evidenceFiles ?? []), ...(baseRequest.evidenceFiles ?? [])].forEach((evidenceFile) => {
    evidenceByKey.set(getEvidenceMergeKey(evidenceFile), evidenceFile);
  });

  return {
    ...secondaryRequest,
    ...baseRequest,
    fieldChanges: baseRequest.fieldChanges.length >= secondaryRequest.fieldChanges.length
      ? baseRequest.fieldChanges
      : secondaryRequest.fieldChanges,
    evidenceFiles: Array.from(evidenceByKey.values()),
    approvals: Array.from(approvalsByKey.values()),
  };
};

const mergeChangeRequestsBySource = (
  results: PromiseSettledResult<NodeChangeRequestResult>[],
  fallbackRequests: ChangeRequest[],
  fallbackMap: Record<string, string>,
) => {
  const requestsById = new Map<string, ChangeRequest>();
  const requestNodeBaseUrls: Record<string, string> = {};

  const addRequest = (request: ChangeRequest, baseUrl?: string) => {
    const existingRequest = requestsById.get(request.id);
    requestsById.set(request.id, existingRequest ? mergeChangeRequest(existingRequest, request) : request);

    if (baseUrl && !requestNodeBaseUrls[request.id]) {
      requestNodeBaseUrls[request.id] = baseUrl;
    }
  };

  results.forEach((result) => {
    if (result.status !== 'fulfilled') {
      return;
    }

    result.value.requests.forEach((request) => addRequest(request, result.value.node.baseUrl));
  });

  fallbackRequests.forEach((request) => addRequest(request, fallbackMap[request.id]));

  const requests = Array.from(requestsById.values()).sort(
    (left, right) => new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime(),
  );

  return { requests, requestNodeBaseUrls };
};

const getApproverProfile = (departmentCode: NodeOption['departmentCode']) => {
  switch (departmentCode) {
    case 1:
      return {
        fullName: 'Naledi Mokoena',
        role: 'Senior Identity Verifier',
        emailAddress: 'naledi.mokoena@dha.gov.za',
      };
    case 2:
      return {
        fullName: 'Thabo Dlamini',
        role: 'Tax Compliance Reviewer',
        emailAddress: 'thabo.dlamini@sars.gov.za',
      };
    case 3:
      return {
        fullName: 'Ayesha Naidoo',
        role: 'Municipal Records Officer',
        emailAddress: 'ayesha.naidoo@municipality.gov.za',
      };
    case 4:
      return {
        fullName: 'Lerato Nkosi',
        role: 'Health Records Reviewer',
        emailAddress: 'lerato.nkosi@health.gov.za',
      };
    case 5:
      return {
        fullName: 'Sipho Khumalo',
        role: 'Safety Records Officer',
        emailAddress: 'sipho.khumalo@saps.gov.za',
      };
    default:
      return {
        fullName: 'Department Approver',
        role: 'Records Officer',
        emailAddress: 'approver@civicsync.gov.za',
      };
  }
};

const buildFallbackDepartmentUsers = (
  activeNode: NodeOption,
  citizens: Citizen[],
  changeRequests: ChangeRequest[],
  ledger: LedgerEntry[],
  outbox: SyncOutboxEvent[],
  inbox: SyncInboxEntry[],
) => {
  const departmentNodeId =
    citizens[0]?.departmentNodeId ||
    outbox[0]?.departmentNodeId ||
    inbox[0]?.departmentNodeId ||
    ledger[0]?.originatingNodeId ||
    changeRequests[0]?.requestedAtNodeId ||
    '';

  if (!departmentNodeId) {
    return [] as DepartmentUser[];
  }

  const approver = getApproverProfile(activeNode.departmentCode);

  return [{
    id: `${departmentNodeId}-${activeNode.departmentCode}`,
    departmentNodeId,
    ...approver,
    isActive: true,
  }] as DepartmentUser[];
};

const PollingIntervalMs = 5000;

export const CivicSyncProvider = ({ children }: { children: React.ReactNode }) => {
  const [state, dispatch] = useReducer(civicSyncReducer, initialState);
  const isPollingRef = useRef(false);
  const client = useMemo(() => new CivicSyncClient(state.activeNode.baseUrl), [state.activeNode.baseUrl]);

  const refreshAll = useCallback(async (showMessage = true) => {
    if (showMessage) {
      dispatch(setOperationPending('Refresh node data'));
    }

    try {
      const results = await Promise.allSettled([
        client.getNodeInfo(),
        client.getCitizens(),
        loadOptionalDepartmentUsers(client),
        client.getChangeRequests(),
        client.getLedger(),
        client.getOutbox(),
        client.getInbox(),
        client.getReceipts(),
      ]);

      const changeRequestResults = await Promise.allSettled(
        nodes.map(async (node) => ({
          node,
          requests: await new CivicSyncClient(node.baseUrl).getChangeRequests(),
        })),
      );

      const nodeInfo = resolveSettledValue<NodeInfo | null>(results[0], state.nodeInfo);
      const citizens = resolveSettledValue<Citizen[]>(results[1], state.citizens);
      const loadedUsers = resolveSettledValue<DepartmentUser[]>(results[2], state.users);
      const activeNodeChangeRequests = resolveSettledValue<ChangeRequest[]>(results[3], state.changeRequests);
      const { requests: changeRequests, requestNodeBaseUrls } = mergeChangeRequestsBySource(
        changeRequestResults,
        activeNodeChangeRequests,
        state.requestNodeBaseUrls,
      );
      const ledger = resolveSettledValue<LedgerEntry[]>(results[4], state.ledger);
      const outbox = resolveSettledValue<SyncOutboxEvent[]>(results[5], state.outbox);
      const inbox = resolveSettledValue<SyncInboxEntry[]>(results[6], state.inbox);
      const receipts = resolveSettledValue<SyncReceipt[]>(results[7], state.receipts);
      const users = loadedUsers.length > 0
        ? loadedUsers
        : buildFallbackDepartmentUsers(state.activeNode, citizens, changeRequests, ledger, outbox, inbox);
      const rejectedMessages = getRejectedMessages([...results, ...changeRequestResults]);
      const hasConnectionFailure = rejectedMessages.length > 0;
      const statusMessage = hasConnectionFailure
        ? `${state.activeNode.name} partially loaded. ${rejectedMessages[0]}`
        : `Connected to ${state.activeNode.name}.`;

      dispatch(setCivicSyncState({
          nodeInfo,
          citizens,
          users,
          changeRequests,
          requestNodeBaseUrls,
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
  }, [client, state.activeNode, state.changeRequests, state.message, state.requestNodeBaseUrls, state.selectedCitizenId, state.selectedRequestId]);

  useEffect(() => {
    refreshAll();
  }, [state.activeNode.baseUrl]);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      if (isPollingRef.current) {
        return;
      }

      isPollingRef.current = true;
      runSilentSyncMaintenance(client)
        .then(() => refreshAll(false))
        .catch(() => {
          // Silent polling keeps the last visible manual action state intact.
        })
        .finally(() => {
          isPollingRef.current = false;
        });
    }, PollingIntervalMs);

    return () => window.clearInterval(intervalId);
  }, [refreshAll]);

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
      case 'EmailAddress':
        return { fieldName: 'ContactDetails', newValue: `${newValue}|${selectedCitizen.phoneNumber}` };
      case 'PhoneNumber':
        return { fieldName: 'ContactDetails', newValue: `${selectedCitizen.emailAddress}|${newValue}` };
      default:
        return { fieldName, newValue };
    }
  };

  const getTargetRequest = (requestId?: string) => {
    return state.changeRequests.find((request) => request.id === (requestId || state.selectedRequestId));
  };

  const getRequestClient = (request: ChangeRequest) => {
    return new CivicSyncClient(state.requestNodeBaseUrls[request.id] ?? state.activeNode.baseUrl);
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
          evidenceFiles: request.evidenceFiles ?? [],
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

      if (!targetRequest) {
        throw new Error('Select a request first.');
      }

      const departmentApproval = findDepartmentApproval(targetRequest, state.activeNode.departmentCode);

      if (departmentApproval) {
        throw new Error('This department has already been asked to approve the selected request.');
      }

      throw new Error('This request is not assigned to this department.');
    }),
    approveRequest: (requestId?: string) => runAction('Approve request', async () => {
      const targetRequest = getTargetRequest(requestId);

      if (!targetRequest) {
        throw new Error('Select a request first.');
      }

      const departmentApproval = findDepartmentApproval(targetRequest, state.activeNode.departmentCode);

      if (!departmentApproval) {
        throw new Error('This request is not assigned to this department.');
      }

      if (!departmentApproval.approverUserId) {
        throw new Error('This request does not have an assigned approver for this department.');
      }

      const requestClient = getRequestClient(targetRequest);
      await requestClient.recordDecision(
        targetRequest.id,
        departmentApproval.approvingNodeId,
        departmentApproval.approverUserId,
        'Approved from CivicSync frontend',
      );
      dispatch(setCivicSyncState({ selectedRequestId: targetRequest.id }));
    }),
    commitRequest: (requestId?: string) => runAction('Commit request', async () => {
      const targetRequest = getTargetRequest(requestId);

      if (!targetRequest) {
        throw new Error('Select a request first.');
      }

      if (targetRequest.status !== 3) {
        throw new Error('Only approved change requests can be committed.');
      }

      await getRequestClient(targetRequest).commitChangeRequest(targetRequest.id);
      dispatch(setCivicSyncState({ selectedRequestId: targetRequest.id }));
    }),
    publishOutbox: () => runAction('Publish outbox', async () => {
      await client.publishOutbox();
    }),
    applyInbox: () => runAction('Apply inbox', async () => {
      await client.applyInbox();
    }),
    enrollBiometric: (descriptor: string) => runAction('Enroll biometric', async () => {
      if (!selectedCitizen) {
        throw new Error('Select or register a citizen before enrolling biometrics.');
      }

      await client.enrollBiometric(selectedCitizen.id, {
        method: 'Face scan',
        deviceLabel: 'Browser camera',
        descriptor,
      });
    }),
    verifyBiometric: (descriptor: string) => runAction('Verify biometric', async () => {
      if (!selectedCitizen) {
        throw new Error('Select or register a citizen before verifying biometrics.');
      }

      const result = await client.verifyBiometric(selectedCitizen.id, {
        method: 'Face scan',
        deviceLabel: 'Browser camera',
        descriptor,
      });

      if (!result.isVerified) {
        throw new Error(result.message);
      }
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

