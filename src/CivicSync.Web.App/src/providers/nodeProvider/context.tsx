import { createContext } from 'react';
import type { NodeInfo, NodeOption } from '../../api/types';

export interface NodeStateContextValue {
  activeNode: NodeOption;
  nodeInfo: NodeInfo | null;
  nodes: NodeOption[];
}

export interface NodeActionContextValue {
  setActiveNode: (node: NodeOption) => void;
  refreshAll: () => Promise<void>;
}

export const NodeStateContext = createContext<NodeStateContextValue | undefined>(undefined);
export const NodeActionContext = createContext<NodeActionContextValue | undefined>(undefined);
