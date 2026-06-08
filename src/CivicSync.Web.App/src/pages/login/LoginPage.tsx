import { Button, Card, Input } from 'antd';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthActions } from '../../providers/authProvider';
import { loginAccounts } from '../../providers/authProvider/context';
import { useCivicSyncActions } from '../../providers/civicSyncProvider';
import { nodes } from '../../providers/civicSyncProvider/context';

const LoginPage = () => {
  const navigate = useNavigate();
  const { signIn } = useAuthActions();
  const { setActiveNode } = useCivicSyncActions();
  const [emailAddress, setEmailAddress] = useState('');
  const [password, setPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState('');

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage('');

    const account = loginAccounts.find((item) =>
      item.emailAddress.toLowerCase() === emailAddress.trim().toLowerCase() &&
      item.password === password,
    );

    if (!account) {
      setErrorMessage('Invalid email address or password.');
      return;
    }

    if (account.profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === account.profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    signIn(account.profile);
    navigate(account.profile.workspacePath, { replace: true });
  };

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div className="login-copy">
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
            {errorMessage && <p className="form-error" role="alert">{errorMessage}</p>}
            <Button className="primary-button" htmlType="submit">Sign in</Button>
          </form>
        </Card>
      </section>
    </main>
  );
};

export default LoginPage;
