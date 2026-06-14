import { NodeActionEnums, type NodeAction } from './actions';
import type { NodeStateContextValue } from './context';

export const nodeReducer = (
  state: NodeStateContextValue,
  action: NodeAction,
): NodeStateContextValue => {
  switch (action.type) {
    case NodeActionEnums.setState:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};
