import { useContext, useMemo, useReducer } from 'react';
import { signInError, signInPending, signInSuccess, signOutSuccess } from './actions';
import { AuthActionContext, AuthStateContext, authStorageKey, initialAuthState, loginAccounts, passkeyStorageKey, type AppUserProfile, type AuthStateContextValue } from './context';
import { authReducer } from './reducer';

interface StoredPasskey {
  emailAddress: string;
  credentialId: string;
}

const base64UrlEncode = (bytes: ArrayBuffer | Uint8Array) => {
  const byteArray = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
  const binary = Array.from(byteArray, (byte) => String.fromCharCode(byte)).join('');
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
};

const base64UrlDecode = (value: string) => {
  const normalized = value.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
  return Uint8Array.from(atob(padded), (character) => character.charCodeAt(0));
};

const randomChallenge = () => {
  const bytes = new Uint8Array(32);
  crypto.getRandomValues(bytes);
  return bytes;
};

const loadStoredPasskeys = (): StoredPasskey[] => {
  const storedValue = window.localStorage.getItem(passkeyStorageKey);
  return storedValue ? JSON.parse(storedValue) as StoredPasskey[] : [];
};

const saveStoredPasskey = (passkey: StoredPasskey) => {
  const passkeys = loadStoredPasskeys().filter(
    (item) => item.emailAddress.toLowerCase() !== passkey.emailAddress.toLowerCase(),
  );
  passkeys.push(passkey);
  window.localStorage.setItem(passkeyStorageKey, JSON.stringify(passkeys));
};

const getPasskeySupportError = () => {
  if (!window.isSecureContext) {
    return 'Passkeys require HTTPS or localhost.';
  }

  if (!window.PublicKeyCredential || !navigator.credentials) {
    return 'This browser does not support platform passkeys.';
  }

  return '';
};

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
    registerPasskey: async (emailAddress: string, password: string) => {
      dispatch(signInPending());

      try {
        const supportError = getPasskeySupportError();
        if (supportError) {
          dispatch(signInError(supportError));
          return null;
        }

        const account = loginAccounts.find((item) =>
          item.emailAddress.toLowerCase() === emailAddress.trim().toLowerCase() &&
          item.password === password,
        );

        if (!account) {
          dispatch(signInError('Enter the account email and password before registering a passkey.'));
          return null;
        }

        const userId = new TextEncoder().encode(account.profile.id);
        const credential = await navigator.credentials.create({
          publicKey: {
            challenge: randomChallenge(),
            rp: {
              name: 'CivicSync Ledger',
            },
            user: {
              id: userId,
              name: account.emailAddress,
              displayName: account.profile.displayName,
            },
            pubKeyCredParams: [
              { type: 'public-key', alg: -7 },
              { type: 'public-key', alg: -257 },
            ],
            authenticatorSelection: {
              authenticatorAttachment: 'platform',
              residentKey: 'preferred',
              userVerification: 'required',
            },
            timeout: 60000,
            attestation: 'none',
          },
        });

        if (!(credential instanceof PublicKeyCredential)) {
          dispatch(signInError('Passkey registration was cancelled.'));
          return null;
        }

        saveStoredPasskey({
          emailAddress: account.emailAddress,
          credentialId: base64UrlEncode(credential.rawId),
        });

        window.localStorage.setItem(authStorageKey, JSON.stringify(account.profile));
        dispatch(signInSuccess(account.profile));
        return account.profile;
      } catch (error) {
        dispatch(signInError(error instanceof Error ? error.message : 'Passkey registration failed.'));
        return null;
      }
    },
    signInWithPasskey: async (emailAddress: string) => {
      dispatch(signInPending());

      try {
        const supportError = getPasskeySupportError();
        if (supportError) {
          dispatch(signInError(supportError));
          return null;
        }

        const account = loginAccounts.find((item) =>
          item.emailAddress.toLowerCase() === emailAddress.trim().toLowerCase(),
        );

        if (!account) {
          dispatch(signInError('Enter a known account email address before using a passkey.'));
          return null;
        }

        const passkey = loadStoredPasskeys().find(
          (item) => item.emailAddress.toLowerCase() === account.emailAddress.toLowerCase(),
        );

        if (!passkey) {
          dispatch(signInError('No passkey is registered for this account on this device.'));
          return null;
        }

        const assertion = await navigator.credentials.get({
          publicKey: {
            challenge: randomChallenge(),
            allowCredentials: [
              {
                type: 'public-key',
                id: base64UrlDecode(passkey.credentialId),
              },
            ],
            userVerification: 'required',
            timeout: 60000,
          },
        });

        if (!(assertion instanceof PublicKeyCredential)) {
          dispatch(signInError('Passkey sign-in was cancelled.'));
          return null;
        }

        window.localStorage.setItem(authStorageKey, JSON.stringify(account.profile));
        dispatch(signInSuccess(account.profile));
        return account.profile;
      } catch (error) {
        dispatch(signInError(error instanceof Error ? error.message : 'Passkey sign-in failed.'));
        return null;
      }
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
