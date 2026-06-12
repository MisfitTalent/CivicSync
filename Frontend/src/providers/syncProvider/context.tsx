import { createContext } from 'react';
import type { LedgerEntry, SyncInboxEntry, SyncOutboxEvent, SyncReceipt } from '../../api/types';

export interface SyncStateContextValue {
  ledger: LedgerEntry[];
  outbox: SyncOutboxEvent[];
  inbox: SyncInboxEntry[];
  receipts: SyncReceipt[];
}

export interface SyncActionContextValue {
  publishOutbox: () => Promise<void>;
  applyInbox: () => Promise<void>;
}

export const SyncStateContext = createContext<SyncStateContextValue | undefined>(undefined);
export const SyncActionContext = createContext<SyncActionContextValue | undefined>(undefined);
