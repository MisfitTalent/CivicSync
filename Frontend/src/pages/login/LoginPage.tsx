import { Button, Card, Input } from 'antd';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { useCivicSyncActions } from '../../providers/civicSyncProvider';
import { nodes } from '../../providers/civicSyncProvider/context';

const LoginPage = () => {
  const navigate = useNavigate();
  const { registerPasskey, signIn, signInWithPasskey } = useAuthActions();
  const authState = useAuthState();
  const { setActiveNode } = useCivicSyncActions();
  const [emailAddress, setEmailAddress] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const profile = signIn(emailAddress, password);

    if (!profile) {
      return;
    }

    if (profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    navigate(profile.workspacePath, { replace: true });
  };

  const completePasskeyFlow = (profile: ReturnType<typeof signIn>) => {
    if (!profile) {
      return;
    }

    if (profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    navigate(profile.workspacePath, { replace: true });
  };

  const handleRegisterPasskey = async () => {
    const profile = await registerPasskey(emailAddress, password);
    completePasskeyFlow(profile);
  };

  const handlePasskeySignIn = async () => {
    const profile = await signInWithPasskey(emailAddress);
    completePasskeyFlow(profile);
  };

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div className="login-copy">
          <img className="login-logo" src="/civicsync-logo.svg" alt="CivicSync Ledger logo" />
          <p className="eyebrow">CivicSync Ledger</p>
          <h1>Sign in</h1>
          <p>Access your CivicSync workspace to manage records, approvals, ledger activity, and department sync.</p>
        </div>

        <Card className="login-card">
          <h2>Account Login</h2>
          <form className="form-stack" onSubmit={handleSubmit}>
            <label>
              <span>Email address</span>
              <Input type="email" value={emailAddress} onChange={(event) => setEmailAddress(event.target.value)} required />
            </label>
            <label>
              <span>Password</span>
              <Input.Password value={password} onChange={(event) => setPassword(event.target.value)} required />
            </label>
            {authState.isError && <p className="form-error" role="alert">{authState.errorMessage}</p>}
            <Button className="primary-button" htmlType="submit" disabled={authState.isPending}>Sign in</Button>
            <div className="passkey-actions">
              <Button type="default" htmlType="button" disabled={authState.isPending || !emailAddress.trim()} onClick={handlePasskeySignIn}>
                Use device passkey
              </Button>
              <Button type="default" htmlType="button" disabled={authState.isPending || !emailAddress.trim() || !password} onClick={handleRegisterPasskey}>
                Register passkey
              </Button>
            </div>
            <p className="helper-text">Passkeys use your device authenticator, such as Windows Hello, Face ID, or fingerprint, without sending biometric data to CivicSync.</p>
          </form>
        </Card>
      </section>
    </main>
  );
};

export default LoginPage;
