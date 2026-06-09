import type { AppUserProfile } from './context';

export enum AuthActionEnums {
  signInPending = 'SIGN_IN_PENDING',
  signInSuccess = 'SIGN_IN_SUCCESS',
  signInError = 'SIGN_IN_ERROR',
  signOutSuccess = 'SIGN_OUT_SUCCESS',
}

export type AuthAction =
  | { type: AuthActionEnums.signInPending }
  | { type: AuthActionEnums.signInSuccess; payload: AppUserProfile }
  | { type: AuthActionEnums.signInError; payload: string }
  | { type: AuthActionEnums.signOutSuccess };

export const signInPending = (): AuthAction => ({
  type: AuthActionEnums.signInPending,
});

export const signInSuccess = (payload: AppUserProfile): AuthAction => ({
  type: AuthActionEnums.signInSuccess,
  payload,
});

export const signInError = (payload: string): AuthAction => ({
  type: AuthActionEnums.signInError,
  payload,
});

export const signOutSuccess = (): AuthAction => ({
  type: AuthActionEnums.signOutSuccess,
});
