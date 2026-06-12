import { useContext, useMemo, type ReactNode } from 'react';
import { useCivicSyncActions, useCivicSyncState } from '../civicSyncProvider';
import { CitizenActionContext, CitizenStateContext } from './context';

export const CitizenProvider = ({ children }: { children: ReactNode }) => {
  const { citizens, selectedCitizenId, citizenForm } = useCivicSyncState();
  const { setSelectedCitizenId, updateCitizenForm, createCitizen } = useCivicSyncActions();
  const selectedCitizen = useMemo(
    () => citizens.find((citizen) => citizen.id === selectedCitizenId),
    [citizens, selectedCitizenId],
  );

  const state = useMemo(
    () => ({ citizens, selectedCitizen, selectedCitizenId, citizenForm }),
    [citizens, selectedCitizen, selectedCitizenId, citizenForm],
  );
  const actions = useMemo(
    () => ({ setSelectedCitizenId, updateCitizenForm, createCitizen }),
    [setSelectedCitizenId, updateCitizenForm, createCitizen],
  );

  return (
    <CitizenStateContext.Provider value={state}>
      <CitizenActionContext.Provider value={actions}>{children}</CitizenActionContext.Provider>
    </CitizenStateContext.Provider>
  );
};

export const useCitizenState = () => {
  const context = useContext(CitizenStateContext);
  if (!context) {
    throw new Error('useCitizenState must be used within CitizenProvider');
  }
  return context;
};

export const useCitizenActions = () => {
  const context = useContext(CitizenActionContext);
  if (!context) {
    throw new Error('useCitizenActions must be used within CitizenProvider');
  }
  return context;
};
