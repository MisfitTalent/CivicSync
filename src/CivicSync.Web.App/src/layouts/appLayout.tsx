import { Button } from 'antd';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../providers/authProvider';
import type { UserRole } from '../providers/authProvider/context';
import { nodes } from '../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../providers/civicSyncProvider';

interface NavigationItem {
  label: string;
  path: string;
  shortLabel: string;
  roles: UserRole[];
}

const navigationItems: NavigationItem[] = [
  { label: 'Citizen Portal', path: '/citizen', shortLabel: 'CP', roles: ['Citizen'] },
  { label: 'Home Affairs', path: '/home-affairs', shortLabel: 'HA', roles: ['HomeAffairsOfficer'] },
  { label: 'SARS', path: '/sars', shortLabel: 'SA', roles: ['SarsOfficer'] },
  { label: 'Municipality', path: '/municipality', shortLabel: 'MU', roles: ['MunicipalityOfficer'] },
  { label: 'Admin Console', path: '/admin', shortLabel: 'AD', roles: ['Admin'] },
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
      <aside className="app-sidebar" aria-label="CivicSync workspace navigation">
        <div className="sidebar-brand">
          <img className="sidebar-logo" src="/civicsync-logo.svg" alt="CivicSync Ledger logo" />
          <div>
            <strong>CivicSync</strong>
            <span>Ledger</span>
          </div>
        </div>

        <nav className="sidebar-nav">
          {visibleNavigationItems.map((item) => (
            <NavLink className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`} key={item.path} to={item.path}>
              <span>{item.shortLabel}</span>
              <strong>{item.label}</strong>
            </NavLink>
          ))}
        </nav>

        {isAdmin && (
          <div className="sidebar-node-selector">
            <span>Node Selector</span>
            {nodes.map((node) => (
              <Button className={node.baseUrl === activeNode.baseUrl ? 'active' : ''} key={node.baseUrl} onClick={() => setActiveNode(node)}>
                {node.name}
              </Button>
            ))}
          </div>
        )}
      </aside>

      <section className="app-main">
        <header className="app-topbar">
          <div>
            <p className="eyebrow">Decentralized public-sector ledger</p>
            <h1>CivicSync Ledger</h1>
          </div>

          <div className="topbar-user-card" aria-live="polite">
            <span>{isLoading ? 'Working' : roleLabel[currentUser?.role ?? 'Citizen']}</span>
            <strong>{currentUser?.displayName ?? 'Unknown user'}</strong>
            <small>{activeNode.name} • {activeNode.baseUrl}</small>
            <Button onClick={handleSignOut}>Sign out</Button>
          </div>
        </header>

        <div className="app-content">
          <Outlet />
        </div>
      </section>
    </div>
  );
};

export default AppLayout;
