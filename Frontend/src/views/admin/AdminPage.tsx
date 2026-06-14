import { Button, Input } from 'antd';
import { useState } from 'react';
import { AuditPanel, Info, Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import CitizenRegistrationPanel from '../../components/workflow/CitizenRegistrationPanel';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { statusText } from '../../providers/civicSyncProvider/context';

const receiptResultText: Record<number, string> = {
  1: 'Applied by peer',
  2: 'Queued for review',
  3: 'Stored for matching',
  4: 'Security check failed',
  5: 'Delivery failed',
};

const AdminPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const authActions = useAuthActions();
  const { currentUser } = useAuthState();
  const [departmentUserName, setDepartmentUserName] = useState('');
  const [departmentUserRole, setDepartmentUserRole] = useState('');
  const [departmentUserEmail, setDepartmentUserEmail] = useState('');
  const [temporaryPassword, setTemporaryPassword] = useState('');
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;
  const canCreateDepartmentLogin = Boolean(
    departmentUserName.trim() &&
    departmentUserRole.trim() &&
    departmentUserEmail.trim() &&
    temporaryPassword.length >= 8 &&
    state.activeNode.departmentCode <= 3,
  );

  const handleCreateDepartmentUser = async () => {
    const createdUser = await actions.createDepartmentUser({
      fullName: departmentUserName,
      role: departmentUserRole,
      emailAddress: departmentUserEmail,
    });
    const loginProfile = authActions.createDepartmentLoginAccount(
      state.activeNode.departmentCode,
      createdUser.id,
      createdUser.fullName,
      createdUser.emailAddress,
      temporaryPassword,
    );

    if (!loginProfile) {
      return;
    }

    setDepartmentUserName('');
    setDepartmentUserRole('');
    setDepartmentUserEmail('');
    setTemporaryPassword('');
  };

  return (
    <main className="page-stack">
      <section className="page-intro">
        <div>
          <p className="eyebrow">Admin workspace</p>
          <h2>System monitoring</h2>
          <p>Admin users monitor nodes, queue sizes, ledger activity, and peer sync outcomes.</p>
        </div>
        <div className="department-metrics">
          <Metric label="Citizens" value={state.citizens.length} />
          <Metric label="Outbox" value={state.outbox.length} />
          <Metric label="Receipts" value={state.receipts.length} />
        </div>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <div className="department-grid">
        <section className="panel span-2">
          <PanelHeader title="Operational Context" actionLabel="Refresh" onAction={actions.refreshAll} />
          <div className="info-grid">
            <Info label="Signed In" value={currentUser?.displayName ?? 'Unknown'} />
            <Info label="Active Node" value={state.activeNode.name} />
            <Info label="Connection" value="Secure department workspace" />
            <Info label="Peer Departments" value={state.nodeInfo?.peers?.length ?? 0} />
          </div>
        </section>

        <section className="panel span-2 admin-user-panel">
          <div className="panel-header">
            <h2>Admin User Provisioning</h2>
          </div>
          <p className="helper-text">Create department users from the selected active node. Public registration cannot create officer accounts.</p>
          <div className="admin-user-form">
            <label>
              <span>Department</span>
              <Input value={state.activeNode.name} readOnly />
            </label>
            <label>
              <span>Full name</span>
              <Input value={departmentUserName} onChange={(event) => setDepartmentUserName(event.target.value)} />
            </label>
            <label>
              <span>Role title</span>
              <Input value={departmentUserRole} onChange={(event) => setDepartmentUserRole(event.target.value)} placeholder="Records Officer" />
            </label>
            <label>
              <span>Email address</span>
              <Input type="email" value={departmentUserEmail} onChange={(event) => setDepartmentUserEmail(event.target.value)} />
            </label>
            <label>
              <span>Temporary password</span>
              <Input.Password value={temporaryPassword} onChange={(event) => setTemporaryPassword(event.target.value)} />
            </label>
          </div>
          <div className="button-row">
            <Button className="primary-button" disabled={!canCreateDepartmentLogin || state.isPending} onClick={handleCreateDepartmentUser}>
              Create department login
            </Button>
          </div>
          <div className="approval-list compact admin-user-list">
            {state.users.map((user) => (
              <div className="approval-wait-row" key={user.id}>
                <span>{user.fullName}<small>{user.emailAddress}</small></span>
                <strong>{user.role}</strong>
              </div>
            ))}
          </div>
        </section>

        <CitizenRegistrationPanel title="Admin Citizen Registration" />
        <div id="ledger">
          <AuditPanel title="Ledger" rows={state.ledger.slice(0, 8).map((entry) => [`Entry ${entry.sequenceNumber}`, 'Verified record change', new Date(entry.createdAtUtc).toLocaleString()])} />
        </div>
        <div id="sync-audit">
          <AuditPanel title="Outbox Queue" rows={state.outbox.slice(0, 8).map((entry) => [statusText[entry.status] ?? 'Queued', entry.retryCount > 0 ? 'Retry scheduled' : 'Ready for delivery', 'Peer departments'])} />
        </div>
        <AuditPanel title="Inbox Queue" rows={state.inbox.slice(0, 8).map((entry) => [statusText[entry.status] ?? 'Received', entry.citizenNationalIdNumber ? 'Citizen record matched' : 'Citizen pending match', entry.appliedAtUtc ? 'Applied' : 'Awaiting review'])} />
        <AuditPanel title="Sync Receipts" rows={state.receipts.slice(0, 8).map((receipt) => [receiptResultText[receipt.result] ?? 'Delivery recorded', 'Peer department', new Date(receipt.receivedAtUtc).toLocaleString()])} />
      </div>
    </main>
  );
};

export default AdminPage;
