import type { NodeStateContextValue } from './context';

export enum NodeActionEnums {
  setState = 'SET_STATE',
}

export type NodeAction = {
  type: NodeActionEnums.setState;
  payload: Partial<NodeStateContextValue>;
};

export const setNodeState = (payload: Partial<NodeStateContextValue>): NodeAction => ({
  type: NodeActionEnums.setState,
  payload,
});
