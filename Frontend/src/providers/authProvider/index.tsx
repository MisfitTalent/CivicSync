import { useContext, useMemo, useReducer } from 'react';
import { CivicSyncClient } from '../../api/civicsyncClient';
import { nodes } from '../civicSyncProvider/context';
import { signInError, signInPending, signInSuccess, signOutSuccess } from './actions';
import { AuthActionContext, AuthStateContext, authStorageKey, biometricCitizenLinkStorageKey, initialAuthState, loginAccounts, registeredAccountsStorageKey, type AppUserProfile, type AuthStateContextValue, type LoginAccount } from './context';
import { authReducer } from './reducer';

const authClient = new CivicSyncClient(nodes[0].baseUrl);
const faceApiDescriptorPrefix = 'face-api-recognition-v1:';

const normalizeEmail = (emailAddress: string) => emailAddress.trim().toLowerCase();

const getStoredRegisteredAccounts = (): LoginAccount[] => {
  try {
    return JSON.parse(window.localStorage.getItem(registeredAccountsStorageKey) || '[]') as LoginAccount[];
  } catch {
    return [];
  }
};

const getLoginAccounts = () => [...loginAccounts, ...getStoredRegisteredAccounts()];

const saveRegisteredAccount = (account: LoginAccount) => {
  const registeredAccounts = getStoredRegisteredAccounts();
  window.localStorage.setItem(registeredAccountsStorageKey, JSON.stringify([...registeredAccounts, account]));
};

const rememberBiometricCitizenLink = (accountId: string, citizenId: string) => {
  const storedLinks = JSON.parse(window.localStorage.getItem(biometricCitizenLinkStorageKey) || '{}') as Record<string, string>;
  window.localStorage.setItem(biometricCitizenLinkStorageKey, JSON.stringify({
    ...storedLinks,
    [accountId]: citizenId,
  }));
};

const getStoredBiometricCitizenLink = (accountId: string) => {
  try {
    const storedLinks = JSON.parse(window.localStorage.getItem(biometricCitizenLinkStorageKey) || '{}') as Record<string, string>;
    return storedLinks[accountId];
  } catch {
    return undefined;
  }
};

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

const getRelyingParty = (rpId: string, rpName: string): PublicKeyCredentialRpEntity => {
  const effectiveRpId = getEffectiveRpId(rpId);
  return effectiveRpId ? { id: effectiveRpId, name: rpName } : { name: rpName };
};

const getAssertionOptions = (options: {
  challenge: string;
  rpId: string;
  allowedCredentialIds: string[];
  timeoutMs: number;
}): PublicKeyCredentialRequestOptions => {
  const effectiveRpId = getEffectiveRpId(options.rpId);
  return {
    challenge: base64UrlDecode(options.challenge),
    ...(effectiveRpId ? { rpId: effectiveRpId } : {}),
    allowCredentials: options.allowedCredentialIds.map((credentialId) => ({
      type: 'public-key',
      id: base64UrlDecode(credentialId),
    })),
    userVerification: 'required',
    timeout: options.timeoutMs,
  };
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

const splitDisplayName = (displayName: string) => {
  const nameParts = displayName.trim().split(/\s+/);
  const firstName = nameParts.shift() || displayName.trim();
  const lastName = nameParts.join(' ') || 'Citizen';

  return { firstName, lastName };
};

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [authState, dispatch] = useReducer(authReducer, initialAuthState, loadInitialAuthState);

  const actions = useMemo(() => ({
    signIn: (emailAddress: string, password: string) => {
      dispatch(signInPending());
      const normalizedEmail = normalizeEmail(emailAddress);

      const account = getLoginAccounts().find((item) =>
        item.emailAddress.toLowerCase() === normalizedEmail &&
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
    registerAccount: async (
      displayName: string,
      emailAddress: string,
      password: string,
      nationalIdNumber: string,
      phoneNumber: string,
      faceDescriptor?: string,
    ) => {
      dispatch(signInPending());

      const normalizedEmail = normalizeEmail(emailAddress);
      const resolvedDisplayName = displayName.trim();
      const resolvedNationalIdNumber = nationalIdNumber.trim();
      const resolvedPhoneNumber = phoneNumber.trim();

      if (!resolvedDisplayName) {
        dispatch(signInError('Enter your full name before registering an account.'));
        return null;
      }

      if (!normalizedEmail) {
        dispatch(signInError('Enter an email address before registering an account.'));
        return null;
      }

      if (password.length < 8) {
        dispatch(signInError('Use a password with at least 8 characters.'));
        return null;
      }

      if (!resolvedNationalIdNumber) {
        dispatch(signInError('Enter a national ID number before registering an account.'));
        return null;
      }

      if (!resolvedPhoneNumber) {
        dispatch(signInError('Enter a phone number before registering an account.'));
        return null;
      }

      const accountExists = getLoginAccounts().some((item) => item.emailAddress.toLowerCase() === normalizedEmail);
      if (accountExists) {
        dispatch(signInError('An account already exists for this email address.'));
        return null;
      }

      try {
        const { firstName, lastName } = splitDisplayName(resolvedDisplayName);
        const citizen = await authClient.createCitizen({
          nationalIdNumber: resolvedNationalIdNumber,
          firstName,
          lastName,
          emailAddress: normalizedEmail,
          phoneNumber: resolvedPhoneNumber,
        });

        const account: LoginAccount = {
          emailAddress: normalizedEmail,
          password,
          linkedNationalIdNumber: citizen.nationalIdNumber,
          profile: {
            id: `citizen-${crypto.randomUUID()}`,
            displayName: resolvedDisplayName,
            role: 'Citizen',
            workspacePath: '/citizen',
          },
        };

        if (faceDescriptor) {
          await authClient.enrollBiometric(citizen.id, {
            method: 'Face scan',
            deviceLabel: 'Browser camera',
            descriptor: faceDescriptor,
          });
          rememberBiometricCitizenLink(account.profile.id, citizen.id);
        }

        saveRegisteredAccount(account);
        window.localStorage.setItem(authStorageKey, JSON.stringify(account.profile));
        dispatch(signInSuccess(account.profile));
        return account.profile;
      } catch (error) {
        dispatch(signInError(error instanceof Error ? error.message : 'Account registration failed.'));
        return null;
      }
    },
    registerPasskey: async (emailAddress: string, password: string) => {
      dispatch(signInPending());

      try {
        const supportError = getPasskeySupportError();
        if (supportError) {
          dispatch(signInError(supportError));
          return null;
        }

        const normalizedEmail = normalizeEmail(emailAddress);
        const account = getLoginAccounts().find((item) =>
          item.emailAddress.toLowerCase() === normalizedEmail &&
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
            rp: getRelyingParty(options.rpId, options.rpName),
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

        const normalizedEmail = normalizeEmail(emailAddress);
        const account = getLoginAccounts().find((item) =>
          item.emailAddress.toLowerCase() === normalizedEmail,
        );

        if (!account) {
          dispatch(signInError('Enter a known account email address before using a passkey.'));
          return null;
        }

        const options = await authClient.beginPasskeyLogin(account.emailAddress);
        const assertion = await navigator.credentials.get({
          publicKey: getAssertionOptions(options),
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
    signInWithFace: async (emailAddress: string, descriptor: string) => {
      dispatch(signInPending());

      try {
        const normalizedEmail = normalizeEmail(emailAddress);
        const account = getLoginAccounts().find((item) =>
          item.emailAddress.toLowerCase() === normalizedEmail,
        );

        if (!account) {
          dispatch(signInError('Enter a known account email address before using face login.'));
          return null;
        }

        const citizens = await authClient.getCitizens();
        const linkedCitizenId = getStoredBiometricCitizenLink(account.profile.id);
        const citizen = citizens.find((item) =>
          item.id === linkedCitizenId ||
          item.emailAddress.toLowerCase() === normalizedEmail ||
          item.nationalIdNumber === account.linkedNationalIdNumber,
        );

        if (!citizen) {
          dispatch(signInError('No citizen record is linked to this account. Confirm the demo citizen exists on Home Affairs.'));
          return null;
        }

        if (!citizen.biometricReference?.includes(faceApiDescriptorPrefix)) {
          dispatch(signInError('This citizen record does not have an enrolled face biometric. Sign in normally, open the Citizen Portal, and use Enroll face first.'));
          return null;
        }

        const result = await authClient.verifyBiometric(citizen.id, {
          method: 'Face scan',
          deviceLabel: 'Browser camera',
          descriptor,
        });

        if (!result.isVerified) {
          dispatch(signInError(result.message || 'Face login was rejected by the server.'));
          return null;
        }

        window.localStorage.setItem(authStorageKey, JSON.stringify(account.profile));
        dispatch(signInSuccess(account.profile));
        return account.profile;
      } catch (error) {
        dispatch(signInError(error instanceof Error ? error.message : 'Face login failed.'));
        return null;
      }
    },
    signOut: () => {
      window.localStorage.removeItem(authStorageKey);
      dispatch(signOutSuccess());
    },
    verifyCurrentPassword: (accountId: string | undefined, password: string) => {
      if (!accountId || !password) {
        return false;
      }

      return getLoginAccounts().some((item) =>
        item.profile.id === accountId &&
        item.password === password,
      );
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
