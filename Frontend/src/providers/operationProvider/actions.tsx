import type { OperationStateContextValue } from './context';

export enum OperationActionEnums {
  setState = 'SET_STATE',
}

export type OperationAction = {
  type: OperationActionEnums.setState;
  payload: Partial<OperationStateContextValue>;
};

export const setOperationState = (payload: Partial<OperationStateContextValue>): OperationAction => ({
  type: OperationActionEnums.setState,
  payload,
});
