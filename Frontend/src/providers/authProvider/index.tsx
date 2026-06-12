import { useContext, useMemo, useReducer } from 'react';
import { CivicSyncClient } from '../../api/civicsyncClient';
import { nodes } from '../civicSyncProvider/context';
import { signInError, signInPending, signInSuccess, signOutSuccess } from './actions';
import { AuthActionContext, AuthStateContext, authStorageKey, initialAuthState, loginAccounts, type AppUserProfile, type AuthStateContextValue } from './context';
import { authReducer } from './reducer';

const authClient = new CivicSyncClient(nodes[0].baseUrl);

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

const getEffectiveRpId = (rpId: string) => {
  const hostName = window.location.hostname;
  return rpId && (hostName === rpId || hostName.endsWith(`.${rpId}`)) ? rpId : undefined;
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

        const options = await authClient.beginPasskeyRegistration(account.emailAddress, account.profile.displayName);
        const credential = await navigator.credentials.create({
          publicKey: {
            challenge: base64UrlDecode(options.challenge),
            rp: {
              id: getEffectiveRpId(options.rpId),
              name: options.rpName,
            },
            user: {
              id: base64UrlDecode(options.userId),
              name: options.userName,
              displayName: options.displayName,
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
            timeout: options.timeoutMs,
            attestation: 'none',
          },
        });

        if (!(credential instanceof PublicKeyCredential) ||
            !(credential.response instanceof AuthenticatorAttestationResponse)) {
          dispatch(signInError('Passkey registration was cancelled.'));
          return null;
        }

        const publicKey = credential.response.getPublicKey();
        if (!publicKey) {
          dispatch(signInError('This browser did not expose the passkey public key for server verification.'));
          return null;
        }

        const result = await authClient.completePasskeyRegistration({
          emailAddress: account.emailAddress,
          credentialId: base64UrlEncode(credential.rawId),
          clientDataJson: base64UrlEncode(credential.response.clientDataJSON),
          publicKey: base64UrlEncode(publicKey),
          publicKeyAlgorithm: credential.response.getPublicKeyAlgorithm(),
        });

        if (!result.isAuthenticated) {
          dispatch(signInError(result.message || 'Passkey registration was rejected by the server.'));
          return null;
        }

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

        const options = await authClient.beginPasskeyLogin(account.emailAddress);
        const assertion = await navigator.credentials.get({
          publicKey: {
            challenge: base64UrlDecode(options.challenge),
            rpId: getEffectiveRpId(options.rpId),
            allowCredentials: options.allowedCredentialIds.map((credentialId) => ({
              type: 'public-key',
              id: base64UrlDecode(credentialId),
            })),
            userVerification: 'required',
            timeout: options.timeoutMs,
          },
        });

        if (!(assertion instanceof PublicKeyCredential) ||
            !(assertion.response instanceof AuthenticatorAssertionResponse)) {
          dispatch(signInError('Passkey sign-in was cancelled.'));
          return null;
        }

        const result = await authClient.completePasskeyLogin({
          emailAddress: account.emailAddress,
          credentialId: base64UrlEncode(assertion.rawId),
          clientDataJson: base64UrlEncode(assertion.response.clientDataJSON),
          authenticatorData: base64UrlEncode(assertion.response.authenticatorData),
          signature: base64UrlEncode(assertion.response.signature),
        });

        if (!result.isAuthenticated) {
          dispatch(signInError(result.message || 'Passkey login was rejected by the server.'));
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
