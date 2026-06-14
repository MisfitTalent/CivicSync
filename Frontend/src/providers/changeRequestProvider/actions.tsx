import type { ChangeRequestStateContextValue } from './context';

export enum ChangeRequestActionEnums {
  setState = 'SET_STATE',
}

export type ChangeRequestAction = {
  type: ChangeRequestActionEnums.setState;
  payload: Partial<ChangeRequestStateContextValue>;
};

export const setChangeRequestState = (payload: Partial<ChangeRequestStateContextValue>): ChangeRequestAction => ({
  type: ChangeRequestActionEnums.setState,
  payload,
});
