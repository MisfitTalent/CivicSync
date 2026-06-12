import { createContext } from 'react';
import type { DepartmentCode } from '../../api/types';

export type UserRole = 'Citizen' | 'HomeAffairsOfficer' | 'SarsOfficer' | 'MunicipalityOfficer' | 'Admin';

export interface AppUserProfile {
  id: string;
  displayName: string;
  role: UserRole;
  departmentCode?: DepartmentCode;
  workspacePath: string;
}

export interface LoginAccount {
  emailAddress: string;
  password: string;
  linkedNationalIdNumber?: string;
  profile: AppUserProfile;
}

export interface AuthStateContextValue {
  currentUser: AppUserProfile | null;
  isPending: boolean;
  isSuccess: boolean;
  isError: boolean;
  errorMessage: string;
  successMessage: string;
}

export interface AuthActionContextValue {
  signIn: (emailAddress: string, password: string) => AppUserProfile | null;
  registerAccount: (displayName: string, emailAddress: string, password: string) => AppUserProfile | null;
  registerPasskey: (emailAddress: string, password: string) => Promise<AppUserProfile | null>;
  signInWithPasskey: (emailAddress: string) => Promise<AppUserProfile | null>;
  signInWithFace: (emailAddress: string, descriptor: string) => Promise<AppUserProfile | null>;
  signOut: () => void;
}

export const authStorageKey = 'civicsync.currentUser';
export const registeredAccountsStorageKey = 'civicsync.registeredAccounts';
export const biometricCitizenLinkStorageKey = 'civicsync.biometricCitizenLinks';

export const loginAccounts: LoginAccount[] = [
  {
    emailAddress: 'citizen@civicsync.local',
    password: 'Password123!',
    linkedNationalIdNumber: '0008282828282',
    profile: {
      id: 'citizen-user',
      displayName: 'Citizen User',
      role: 'Citizen',
      workspacePath: '/citizen',
    },
  },
  {
    emailAddress: 'homeaffairs@civicsync.local',
    password: 'Password123!',
    profile: {
      id: 'home-affairs-officer',
      displayName: 'Home Affairs Officer',
      role: 'HomeAffairsOfficer',
      departmentCode: 1,
      workspacePath: '/home-affairs',
    },
  },
  {
    emailAddress: 'sars@civicsync.local',
    password: 'Password123!',
    profile: {
      id: 'sars-officer',
      displayName: 'SARS Officer',
      role: 'SarsOfficer',
      departmentCode: 2,
      workspacePath: '/sars',
    },
  },
  {
    emailAddress: 'municipality@civicsync.local',
    password: 'Password123!',
    profile: {
      id: 'municipality-officer',
      displayName: 'Municipality Officer',
      role: 'MunicipalityOfficer',
      departmentCode: 3,
      workspacePath: '/municipality',
    },
  },
  {
    emailAddress: 'admin@civicsync.local',
    password: 'Password123!',
    profile: {
      id: 'system-admin',
      displayName: 'System Administrator',
      role: 'Admin',
      workspacePath: '/admin',
    },
  },
];

export const initialAuthState: AuthStateContextValue = {
  currentUser: null,
  isPending: false,
  isSuccess: false,
  isError: false,
  errorMessage: '',
  successMessage: '',
};

export const AuthStateContext = createContext<AuthStateContextValue | undefined>(undefined);
export const AuthActionContext = createContext<AuthActionContextValue | undefined>(undefined);
