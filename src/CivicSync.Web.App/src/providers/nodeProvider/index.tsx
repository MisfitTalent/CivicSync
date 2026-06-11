import { useContext, useMemo, type ReactNode } from 'react';
import { useCivicSyncActions, useCivicSyncState } from '../civicSyncProvider';
import { nodes } from '../civicSyncProvider/context';
import { NodeActionContext, NodeStateContext } from './context';

export const NodeProvider = ({ children }: { children: ReactNode }) => {
  const { activeNode, nodeInfo } = useCivicSyncState();
  const { setActiveNode, refreshAll } = useCivicSyncActions();

  const state = useMemo(() => ({ activeNode, nodeInfo, nodes }), [activeNode, nodeInfo]);
  const actions = useMemo(() => ({ setActiveNode, refreshAll }), [setActiveNode, refreshAll]);

  return (
    <NodeStateContext.Provider value={state}>
      <NodeActionContext.Provider value={actions}>{children}</NodeActionContext.Provider>
    </NodeStateContext.Provider>
  );
};

export const useNodeState = () => {
  const context = useContext(NodeStateContext);
  if (!context) {
    throw new Error('useNodeState must be used within NodeProvider');
  }
  return context;
};

export const useNodeActions = () => {
  const context = useContext(NodeActionContext);
  if (!context) {
    throw new Error('useNodeActions must be used within NodeProvider');
  }
  return context;
};

