import { useContext, useMemo, type ReactNode } from 'react';
import { useCivicSyncActions, useCivicSyncState } from '../civicSyncProvider';
import { SyncActionContext, SyncStateContext } from './context';

export const SyncProvider = ({ children }: { children: ReactNode }) => {
  const { ledger, outbox, inbox, receipts } = useCivicSyncState();
  const { publishOutbox, applyInbox } = useCivicSyncActions();

  const state = useMemo(() => ({ ledger, outbox, inbox, receipts }), [ledger, outbox, inbox, receipts]);
  const actions = useMemo(() => ({ publishOutbox, applyInbox }), [publishOutbox, applyInbox]);

  return (
    <SyncStateContext.Provider value={state}>
      <SyncActionContext.Provider value={actions}>{children}</SyncActionContext.Provider>
    </SyncStateContext.Provider>
  );
};

export const useSyncState = () => {
  const context = useContext(SyncStateContext);
  if (!context) {
    throw new Error('useSyncState must be used within SyncProvider');
  }
  return context;
};

export const useSyncActions = () => {
  const context = useContext(SyncActionContext);
  if (!context) {
    throw new Error('useSyncActions must be used within SyncProvider');
  }
  return context;
};
