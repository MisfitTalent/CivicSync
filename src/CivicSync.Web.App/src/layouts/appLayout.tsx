import { Button } from 'antd';
import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../providers/authProvider';
import type { UserRole } from '../providers/authProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../providers/civicSyncProvider';
import { nodes } from '../providers/civicSyncProvider/context';

interface NavigationItem {
  label: string;
  path: string;
  roles: UserRole[];
  isPrimary?: boolean;
}

const navigationItems: NavigationItem[] = [
  { label: 'Citizen Portal', path: '/citizen', roles: ['Citizen'], isPrimary: true },
  { label: 'Update Requests', path: '/citizen#update-requests', roles: ['Citizen'] },
  { label: 'Ledger', path: '/citizen#request-history', roles: ['Citizen'] },
  { label: 'Departments', path: '/home-affairs', roles: ['HomeAffairsOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/home-affairs#approvals', roles: ['HomeAffairsOfficer'] },
  { label: 'Ledger', path: '/home-affairs#ledger', roles: ['HomeAffairsOfficer'] },
  { label: 'Departments', path: '/sars', roles: ['SarsOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/sars#approvals', roles: ['SarsOfficer'] },
  { label: 'Ledger', path: '/sars#ledger', roles: ['SarsOfficer'] },
  { label: 'Departments', path: '/municipality', roles: ['MunicipalityOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/municipality#approvals', roles: ['MunicipalityOfficer'] },
  { label: 'Ledger', path: '/municipality#ledger', roles: ['MunicipalityOfficer'] },
  { label: 'Admin Console', path: '/admin', roles: ['Admin'], isPrimary: true },
  { label: 'Ledger', path: '/admin#ledger', roles: ['Admin'] },
  { label: 'Sync Audit', path: '/admin#sync-audit', roles: ['Admin'] },
];

const roleLabel: Record<UserRole, string> = {
  Citizen: 'Citizen User',
  HomeAffairsOfficer: 'Home Affairs',
  SarsOfficer: 'SARS',
  MunicipalityOfficer: 'Municipality',
  Admin: 'System Admin',
};

const AppLayout = () => {
  const navigate = useNavigate();
  const { currentUser } = useAuthState();
  const { signOut } = useAuthActions();
  const { activeNode, isLoading } = useCivicSyncState();
  const { setActiveNode } = useCivicSyncActions();
  const isAdmin = currentUser?.role === 'Admin';
  const visibleNavigationItems = navigationItems.filter((item) => currentUser && item.roles.includes(currentUser.role));

  const handleSignOut = () => {
    signOut();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app-shell">
      <header className="app-topbar">
        <div className="topbar-brand">
          <img className="topbar-logo" src="/civicsync-logo.svg" alt="CivicSync Ledger logo" />
          <div>
            <strong>CivicSync Ledger</strong>
            <span>Decentralized Public Sector Ledger</span>
          </div>
        </div>

        <nav className="topbar-nav" aria-label="CivicSync workspace navigation">
          {visibleNavigationItems.map((item) => (
            <Link className={`topbar-link ${item.isPrimary ? 'active' : ''}`} key={item.path} to={item.path}>
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="topbar-actions">
          {isAdmin && (
            <div className="topbar-node-selector" aria-label="Department node selector">
              {nodes.map((node) => (
                <Button className={node.baseUrl === activeNode.baseUrl ? 'active' : ''} key={node.baseUrl} onClick={() => setActiveNode(node)}>
                  {node.name}
                </Button>
              ))}
            </div>
          )}

          <div className="topbar-user-card" aria-live="polite">
            <span>{isLoading ? 'Working' : roleLabel[currentUser?.role ?? 'Citizen']}</span>
            <strong>{currentUser?.displayName ?? 'Unknown user'}</strong>
            <small>
              {activeNode.name} • {activeNode.baseUrl}
            </small>
          </div>

          <Button className="sign-out-button" onClick={handleSignOut}>
            Sign out
          </Button>
        </div>
      </header>

      <main className="app-main">
        <div className="app-content">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default AppLayout;
