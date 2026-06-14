import { CitizenActionEnums, type CitizenAction } from './actions';
import type { CitizenStateContextValue } from './context';

export const citizenReducer = (
  state: CitizenStateContextValue,
  action: CitizenAction,
): CitizenStateContextValue => {
  switch (action.type) {
    case CitizenActionEnums.setState:
      return { ...state, ...action.payload };
    default:
      return state;
  }
};
