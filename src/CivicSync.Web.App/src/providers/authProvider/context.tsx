import { createContext } from 'react';
import type { DepartmentCode } from '../../api/types';

export type UserRole = 'Citizen' | 'HomeAffairsOfficer' | 'SarsOfficer' | 'MunicipalityOfficer' | 'Admin';

export interface AppUserProfile {
  id: string;
  displayName: string;
  role: UserRole;
  departmentCode?: DepartmentCode;
  workspacePath: string;
  visibleFields: string[];
  capabilities: string[];
}

export interface AuthStateContextValue {
  currentUser: AppUserProfile | null;
}

export interface AuthActionContextValue {
  signIn: (profile: AppUserProfile) => void;
  signOut: () => void;
}

export const demoProfiles: AppUserProfile[] = [
  {
    id: 'citizen-demo',
    displayName: 'Citizen User',
    role: 'Citizen',
    workspacePath: '/citizen',
    visibleFields: ['Own citizen profile', 'Own contact details', 'Own change requests'],
    capabilities: ['Create citizen profile', 'Submit contact change request'],
  },
  {
    id: 'home-affairs-demo',
    displayName: 'Home Affairs Officer',
    role: 'HomeAffairsOfficer',
    departmentCode: 1,
    workspacePath: '/home-affairs',
    visibleFields: ['National ID', 'Full name', 'Date of birth', 'Citizen status'],
    capabilities: ['Approve identity-related changes', 'Commit approved ledger entries', 'Publish peer sync'],
  },
  {
    id: 'sars-demo',
    displayName: 'SARS Officer',
    role: 'SarsOfficer',
    departmentCode: 2,
    workspacePath: '/sars',
    visibleFields: ['Tax number', 'Tax address', 'Employment status', 'Tax-impacting contact changes'],
    capabilities: ['Review tax-impacting changes', 'Apply received inbox updates', 'Inspect local ledger'],
  },
  {
    id: 'municipality-demo',
    displayName: 'Municipality Officer',
    role: 'MunicipalityOfficer',
    departmentCode: 3,
    workspacePath: '/municipality',
    visibleFields: ['Residential address', 'Ward number', 'Service account status', 'Contact details'],
    capabilities: ['Approve residence/contact changes', 'Apply received inbox updates', 'Inspect municipal ledger'],
  },
  {
    id: 'admin-demo',
    displayName: 'System Administrator',
    role: 'Admin',
    workspacePath: '/admin',
    visibleFields: ['Node health', 'Peer configuration', 'Sync audit', 'Operational counts'],
    capabilities: ['Monitor all node status', 'Refresh audit data', 'Check sync queues'],
  },
];

export const AuthStateContext = createContext<AuthStateContextValue | undefined>(undefined);
export const AuthActionContext = createContext<AuthActionContextValue | undefined>(undefined);
