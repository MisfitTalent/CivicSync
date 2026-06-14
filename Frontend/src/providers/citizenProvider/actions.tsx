import type { CitizenStateContextValue } from './context';

export enum CitizenActionEnums {
  setState = 'SET_STATE',
}

export type CitizenAction = {
  type: CitizenActionEnums.setState;
  payload: Partial<CitizenStateContextValue>;
};

export const setCitizenState = (payload: Partial<CitizenStateContextValue>): CitizenAction => ({
  type: CitizenActionEnums.setState,
  payload,
});
