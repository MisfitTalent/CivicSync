import { useContext, useMemo, useReducer } from 'react';
import { signInError, signInPending, signInSuccess, signOutSuccess } from './actions';
import { AuthActionContext, AuthStateContext, authStorageKey, initialAuthState, loginAccounts, type AppUserProfile, type AuthStateContextValue } from './context';
import { authReducer } from './reducer';

const loadStoredUser = (): AppUserProfile | null => {
  const storedValue = window.localStorage.getItem(authStorageKey);
  return storedValue ? JSON.parse(storedValue) as AppUserProfile : null;
};

const loadInitialAuthState = (): AuthStateContextValue => ({
  ...initialAuthState,
  currentUser: loadStoredUser(),
});

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [authState, dispatch] = useReducer(authReducer, initialAuthState, loadInitialAuthState);

  const actions = useMemo(() => ({
    signIn: (emailAddress: string, password: string) => {
      dispatch(signInPending());

      const account = loginAccounts.find((item) =>
        item.emailAddress.toLowerCase() === emailAddress.trim().toLowerCase() &&
        item.password === password,
      );

      if (!account) {
        dispatch(signInError('Invalid email address or password.'));
        return null;
      }

      window.localStorage.setItem(authStorageKey, JSON.stringify(account.profile));
      dispatch(signInSuccess(account.profile));
      return account.profile;
    },
    signOut: () => {
      window.localStorage.removeItem(authStorageKey);
      dispatch(signOutSuccess());
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
