import { createContext } from 'react';
import type { Citizen } from '../../api/types';
import type { CitizenFormState } from '../civicSyncProvider/context';

export interface CitizenStateContextValue {
  citizens: Citizen[];
  selectedCitizen?: Citizen;
  selectedCitizenId: string;
  citizenForm: CitizenFormState;
}

export interface CitizenActionContextValue {
  setSelectedCitizenId: (id: string) => void;
  updateCitizenForm: (values: Partial<CitizenFormState>) => void;
  createCitizen: () => Promise<void>;
}

export const CitizenStateContext = createContext<CitizenStateContextValue | undefined>(undefined);
export const CitizenActionContext = createContext<CitizenActionContextValue | undefined>(undefined);
