import { useContext, useMemo, useState } from 'react';
import { AuthActionContext, AuthStateContext, loginAccounts, type AppUserProfile, type AuthStateContextValue } from './context';

const storageKey = 'civicsync.currentUser';

const loadStoredUser = (): AppUserProfile | null => {
  const storedValue = window.localStorage.getItem(storageKey);
  return storedValue ? JSON.parse(storedValue) as AppUserProfile : null;
};

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [authState, setAuthState] = useState<AuthStateContextValue>(() => ({
    currentUser: loadStoredUser(),
    isPending: false,
    isSuccess: false,
    isError: false,
    errorMessage: '',
    successMessage: '',
  }));

  const actions = useMemo(() => ({
    signIn: (emailAddress: string, password: string) => {
      setAuthState((current) => ({
        ...current,
        isPending: true,
        isSuccess: false,
        isError: false,
        errorMessage: '',
        successMessage: '',
      }));

      const account = loginAccounts.find((item) =>
        item.emailAddress.toLowerCase() === emailAddress.trim().toLowerCase() &&
        item.password === password,
      );

      if (!account) {
        setAuthState((current) => ({
          ...current,
          isPending: false,
          isSuccess: false,
          isError: true,
          errorMessage: 'Invalid email address or password.',
          successMessage: '',
        }));
        return null;
      }

      window.localStorage.setItem(storageKey, JSON.stringify(account.profile));
      setAuthState({
        currentUser: account.profile,
        isPending: false,
        isSuccess: true,
        isError: false,
        errorMessage: '',
        successMessage: `Signed in as ${account.profile.displayName}.`,
      });
      return account.profile;
    },
    signOut: () => {
      window.localStorage.removeItem(storageKey);
      setAuthState({
        currentUser: null,
        isPending: false,
        isSuccess: false,
        isError: false,
        errorMessage: '',
        successMessage: '',
      });
    },
  }), []);

  return (
    <AuthStateContext.Provider value={authState}>
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
