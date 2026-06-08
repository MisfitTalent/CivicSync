import { Button, Card } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { demoProfiles } from '../../providers/authProvider/context';
import { useCivicSyncActions } from '../../providers/civicSyncProvider';
import { nodes } from '../../providers/civicSyncProvider/context';

const LoginPage = () => {
  const navigate = useNavigate();
  const { currentUser } = useAuthState();
  const { signIn } = useAuthActions();
  const { setActiveNode } = useCivicSyncActions();

  const handleSignIn = (profileId: string) => {
    const profile = demoProfiles.find((item) => item.id === profileId);
    if (!profile) {
      return;
    }

    if (profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    signIn(profile);
    navigate(profile.workspacePath, { replace: true });
  };

  return (
    <main className="login-shell">
      <section className="login-hero">
        <p className="eyebrow">CivicSync access</p>
        <h1>Choose a demo profile</h1>
        <p>Each profile sees a different workspace, different permitted actions, and different field visibility.</p>
        {currentUser && <p className="helper-text">Currently signed in as {currentUser.displayName}.</p>}
      </section>

      <section className="profile-grid">
        {demoProfiles.map((profile) => (
          <Card className="profile-card" key={profile.id}>
            <span>{profile.role}</span>
            <h2>{profile.displayName}</h2>
            <div className="profile-section">
              <strong>Can see</strong>
              <ul>
                {profile.visibleFields.map((field) => <li key={field}>{field}</li>)}
              </ul>
            </div>
            <div className="profile-section">
              <strong>Can do</strong>
              <ul>
                {profile.capabilities.map((capability) => <li key={capability}>{capability}</li>)}
              </ul>
            </div>
            <Button className="primary-button" onClick={() => handleSignIn(profile.id)}>Sign in</Button>
          </Card>
        ))}
      </section>
    </main>
  );
};

export default LoginPage;
