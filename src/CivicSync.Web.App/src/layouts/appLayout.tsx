import { Button } from 'antd';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../providers/authProvider';
import type { UserRole } from '../providers/authProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../providers/civicSyncProvider';
import { nodes } from '../providers/civicSyncProvider/context';

interface NavigationItem {
  label: string;
  path: string;
  roles: UserRole[];
  isPrimary?: boolean;
  badgeKey?: 'inbox';
}

const navigationItems: NavigationItem[] = [
  { label: 'Citizen Portal', path: '/citizen', roles: ['Citizen'], isPrimary: true },
  { label: 'Update Requests', path: '/citizen/request-update', roles: ['Citizen'] },
  { label: 'Ledger', path: '/citizen/ledger', roles: ['Citizen'] },
  { label: 'Departments', path: '/home-affairs', roles: ['HomeAffairsOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/home-affairs/requests', roles: ['HomeAffairsOfficer'] },
  { label: 'Inbox', path: '/home-affairs/inbox', roles: ['HomeAffairsOfficer'], badgeKey: 'inbox' },
  { label: 'Ledger', path: '/home-affairs/ledger', roles: ['HomeAffairsOfficer'] },
  { label: 'Departments', path: '/sars', roles: ['SarsOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/sars/requests', roles: ['SarsOfficer'] },
  { label: 'Inbox', path: '/sars/inbox', roles: ['SarsOfficer'], badgeKey: 'inbox' },
  { label: 'Ledger', path: '/sars/ledger', roles: ['SarsOfficer'] },
  { label: 'Departments', path: '/municipality', roles: ['MunicipalityOfficer'], isPrimary: true },
  { label: 'Update Requests', path: '/municipality/requests', roles: ['MunicipalityOfficer'] },
  { label: 'Inbox', path: '/municipality/inbox', roles: ['MunicipalityOfficer'], badgeKey: 'inbox' },
  { label: 'Ledger', path: '/municipality/ledger', roles: ['MunicipalityOfficer'] },
  { label: 'Admin Console', path: '/admin', roles: ['Admin'], isPrimary: true },
  { label: 'Inbox', path: '/admin/inbox', roles: ['Admin'], badgeKey: 'inbox' },
  { label: 'Ledger', path: '/admin/ledger', roles: ['Admin'] },
  { label: 'Sync Audit', path: '/admin/sync-audit', roles: ['Admin'] },
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
  const location = useLocation();
  const { currentUser } = useAuthState();
  const { signOut } = useAuthActions();
  const { activeNode, inbox, isLoading } = useCivicSyncState();
  const { setActiveNode } = useCivicSyncActions();
  const isAdmin = currentUser?.role === 'Admin';
  const visibleNavigationItems = navigationItems.filter((item) => currentUser && item.roles.includes(currentUser.role));
  const currentRoute = `${location.pathname}${location.hash}`;
  const unreadInboxCount = inbox.filter((entry) => entry.status !== 4).length;
  const isNavigationItemActive = (item: NavigationItem) => {
    if (item.path.includes('#')) {
      return item.path === currentRoute;
    }

    return item.path === location.pathname;
  };

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
            <Link className={`topbar-link ${isNavigationItemActive(item) ? 'active' : ''}`} key={item.path} to={item.path}>
              {item.label}
              {item.badgeKey === 'inbox' && unreadInboxCount > 0 && <span className="nav-count-badge">{unreadInboxCount}</span>}
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
