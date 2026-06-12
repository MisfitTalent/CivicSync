import { AuthActionEnums, type AuthAction } from './actions';
import { initialAuthState, type AuthStateContextValue } from './context';

export const authReducer = (state: AuthStateContextValue, action: AuthAction): AuthStateContextValue => {
  switch (action.type) {
    case AuthActionEnums.signInPending:
      return {
        ...state,
        isPending: true,
        isSuccess: false,
        isError: false,
        errorMessage: '',
        successMessage: '',
      };
    case AuthActionEnums.signInSuccess:
      return {
        currentUser: action.payload,
        isPending: false,
        isSuccess: true,
        isError: false,
        errorMessage: '',
        successMessage: `Signed in as ${action.payload.displayName}.`,
      };
    case AuthActionEnums.signInError:
      return {
        ...state,
        isPending: false,
        isSuccess: false,
        isError: true,
        errorMessage: action.payload,
        successMessage: '',
      };
    case AuthActionEnums.signOutSuccess:
      return initialAuthState;
    default:
      return state;
  }
};
