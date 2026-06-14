import { ChangeRequestActionEnums, type ChangeRequestAction } from './actions';
import type { ChangeRequestStateContextValue } from './context';

export const changeRequestReducer = (
  state: ChangeRequestStateContextValue,
  action: ChangeRequestAction,
): ChangeRequestStateContextValue => {
  switch (action.type) {
    case ChangeRequestActionEnums.setState:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};
