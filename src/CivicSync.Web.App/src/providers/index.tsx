import type { ReactNode } from 'react';
import { AuthProvider } from './authProvider';
import { ChangeRequestProvider } from './changeRequestProvider';
import { CitizenProvider } from './citizenProvider';
import { CivicSyncProvider } from './civicSyncProvider';
import { NodeProvider } from './nodeProvider';
import { OperationProvider } from './operationProvider';
import { SyncProvider } from './syncProvider';

export const AppProviders = ({ children }: { children: ReactNode }) => (
  <AuthProvider>
    <CivicSyncProvider>
      <OperationProvider>
        <NodeProvider>
          <CitizenProvider>
            <ChangeRequestProvider>
              <SyncProvider>{children}</SyncProvider>
            </ChangeRequestProvider>
          </CitizenProvider>
        </NodeProvider>
      </OperationProvider>
    </CivicSyncProvider>
  </AuthProvider>
);

export * from './authProvider';
export * from './civicSyncProvider';
export * from './nodeProvider';
export * from './citizenProvider';
export * from './changeRequestProvider';
export * from './syncProvider';
export * from './operationProvider';
