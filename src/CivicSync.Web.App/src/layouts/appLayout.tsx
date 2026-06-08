import { Button } from 'antd';
import { Outlet, useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../providers/authProvider';
import { nodes } from '../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../providers/civicSyncProvider';

const AppLayout = () => {
  const navigate = useNavigate();
  const { currentUser } = useAuthState();
  const { signOut } = useAuthActions();
  const { activeNode, isLoading } = useCivicSyncState();
  const { setActiveNode } = useCivicSyncActions();
  const isAdmin = currentUser?.role === 'Admin';

  const handleSignOut = () => {
    signOut();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app-shell">
      <header className="hero">
        <div className="brand-lockup">
          <img className="brand-logo" src="/civicsync-logo.svg" alt="CivicSync Ledger logo" />
          <div>
            <p className="eyebrow">Decentralized public-sector ledger</p>
            <h1>CivicSync Ledger</h1>
            <p className="hero-copy">Signed-in workspace for citizen requests, department approvals, ledger commits, and peer sync.</p>
          </div>
        </div>
        <div className="hero-card" aria-live="polite">
          <span>{isLoading ? 'Working' : 'Signed in as'}</span>
          <strong>{currentUser?.displayName ?? 'Unknown user'}</strong>
          <small>{activeNode.name} • {activeNode.baseUrl}</small>
          <Button onClick={handleSignOut}>Sign out</Button>
        </div>
      </header>

      {isAdmin && (
        <nav className="node-tabs" aria-label="Admin node selector">
          {nodes.map((node) => (
            <Button className={node.baseUrl === activeNode.baseUrl ? 'active' : ''} key={node.baseUrl} onClick={() => setActiveNode(node)}>
              {node.name} Node
            </Button>
          ))}
        </nav>
      )}

      <Outlet />
    </div>
  );
};

export default AppLayout;
