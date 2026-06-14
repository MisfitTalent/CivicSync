import { OperationActionEnums, type OperationAction } from './actions';
import type { OperationStateContextValue } from './context';

export const operationReducer = (
  state: OperationStateContextValue,
  action: OperationAction,
): OperationStateContextValue => {
  switch (action.type) {
    case OperationActionEnums.setState:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};
