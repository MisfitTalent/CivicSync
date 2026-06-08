import { useContext, useMemo, useState } from 'react';
import { AuthActionContext, AuthStateContext, type AppUserProfile } from './context';

const storageKey = 'civicsync.currentUser';

const loadStoredUser = (): AppUserProfile | null => {
  const storedValue = window.localStorage.getItem(storageKey);
  return storedValue ? JSON.parse(storedValue) as AppUserProfile : null;
};

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [currentUser, setCurrentUser] = useState<AppUserProfile | null>(() => loadStoredUser());

  const actions = useMemo(() => ({
    signIn: (profile: AppUserProfile) => {
      window.localStorage.setItem(storageKey, JSON.stringify(profile));
      setCurrentUser(profile);
    },
    signOut: () => {
      window.localStorage.removeItem(storageKey);
      setCurrentUser(null);
    },
  }), []);

  return (
    <AuthStateContext.Provider value={{ currentUser }}>
      <AuthActionContext.Provider value={actions}>{children}</AuthActionContext.Provider>
    </AuthStateContext.Provider>
  );
};

export const useAuthState = () => {
  const context = useContext(AuthStateContext);
  if (!context) {
    throw new Error('useAuthState must be used within AuthProvider');
  }
  return context;
};

export const useAuthActions = () => {
  const context = useContext(AuthActionContext);
  if (!context) {
    throw new Error('useAuthActions must be used within AuthProvider');
  }
  return context;
};
