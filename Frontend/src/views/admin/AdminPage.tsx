import { Button, Input } from 'antd';
import { useCallback, useEffect, useState } from 'react';
import { CivicSyncClient } from '../../api/civicsyncClient';
import type { ApplyInboxResponse, NodeOption, PublishOutboxResponse } from '../../api/types';
import { AuditPanel, Info, Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import CitizenRegistrationPanel from '../../components/workflow/CitizenRegistrationPanel';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { nodes } from '../../providers/civicSyncProvider/context';
import { getErrorMessage } from '../../utils/axiosInstance';

const receiptResultText: Record<number, string> = {
  1: 'Applied by peer',
  2: 'Queued for review',
  3: 'Stored for matching',
  4: 'Security check failed',
  5: 'Delivery failed',
};

const syncStatusText: Record<number, string> = {
  1: 'Pending',
  2: 'Published',
  3: 'Received',
  4: 'Applied',
  5: 'Failed',
};

type AdminNodeStatus = {
  node: NodeOption;
  health: 'Checking' | 'Online' | 'Offline';
  citizens: number;
  pendingOutbox: number;
  failedOutbox: number;
  pendingInbox: number;
  receipts: number;
  message: string;
};

const createDefaultNodeStatus = (node: NodeOption): AdminNodeStatus => ({
  node,
  health: 'Checking',
  citizens: 0,
  pendingOutbox: 0,
  failedOutbox: 0,
  pendingInbox: 0,
  receipts: 0,
  message: 'Checking node...',
});

const getSyncSummary = (publishResult?: PublishOutboxResponse, applyResult?: ApplyInboxResponse) => {
  const publishText = publishResult
    ? `${publishResult.processedOutboxEvents} outbox processed, ${publishResult.successfulPeerDeliveries} peer deliveries`
    : 'Publish not run';
  const applyText = applyResult
    ? `${applyResult.appliedInboxEntries} inbox applied`
    : 'Apply not run';

  return `${publishText}. ${applyText}.`;
};

const AdminPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const authActions = useAuthActions();
  const { currentUser } = useAuthState();
  const [nodeStatuses, setNodeStatuses] = useState<AdminNodeStatus[]>(nodes.map(createDefaultNodeStatus));
  const [nodeOperationMessage, setNodeOperationMessage] = useState('');
  const [nodeOperationError, setNodeOperationError] = useState('');
  const [activeNodeOperation, setActiveNodeOperation] = useState('');
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

  const loadNodeStatus = useCallback(async (node: NodeOption): Promise<AdminNodeStatus> => {
    const client = new CivicSyncClient(node.baseUrl);
    const [health, citizens, outbox, inbox, receipts] = await Promise.allSettled([
      client.getHealth(),
      client.getCitizens(),
      client.getOutbox(),
      client.getInbox(),
      client.getReceipts(),
    ]);

    const rejected = [health, citizens, outbox, inbox, receipts].find((result) => result.status === 'rejected') as PromiseRejectedResult | undefined;
    const loadedCitizens = citizens.status === 'fulfilled' ? citizens.value : [];
    const loadedOutbox = outbox.status === 'fulfilled' ? outbox.value : [];
    const loadedInbox = inbox.status === 'fulfilled' ? inbox.value : [];
    const loadedReceipts = receipts.status === 'fulfilled' ? receipts.value : [];

    return {
      node,
      health: health.status === 'fulfilled' && health.value.status === 'ok' ? 'Online' : 'Offline',
      citizens: loadedCitizens.length,
      pendingOutbox: loadedOutbox.filter((entry) => entry.status !== 2).length,
      failedOutbox: loadedOutbox.filter((entry) => entry.status === 5).length,
      pendingInbox: loadedInbox.filter((entry) => entry.status !== 4).length,
      receipts: loadedReceipts.length,
      message: rejected ? getErrorMessage(rejected.reason) : 'Node responding normally',
    };
  }, []);

  const refreshNodeStatuses = useCallback(async () => {
    setNodeOperationError('');
    setNodeStatuses(nodes.map(createDefaultNodeStatus));
    const statuses = await Promise.all(nodes.map(loadNodeStatus));
    setNodeStatuses(statuses);
  }, [loadNodeStatus]);

  useEffect(() => {
    refreshNodeStatuses();
  }, [refreshNodeStatuses]);

  const runNodeOperation = async (
    node: NodeOption,
    operationName: string,
    operation: (client: CivicSyncClient) => Promise<PublishOutboxResponse | ApplyInboxResponse | [PublishOutboxResponse, ApplyInboxResponse]>,
  ) => {
    const operationKey = `${node.departmentCode}:${operationName}`;
    setActiveNodeOperation(operationKey);
    setNodeOperationMessage('');
    setNodeOperationError('');

    try {
      const result = await operation(new CivicSyncClient(node.baseUrl));
      const [publishResult, applyResult] = Array.isArray(result) ? result : operationName === 'Publish outbox' ? [result as PublishOutboxResponse, undefined] : [undefined, result as ApplyInboxResponse];
      setNodeOperationMessage(`${node.name}: ${getSyncSummary(publishResult, applyResult)}`);
      await refreshNodeStatuses();
      if (node.departmentCode === state.activeNode.departmentCode) {
        await actions.refreshAll();
      }
    } catch (error) {
      setNodeOperationError(`${node.name}: ${getErrorMessage(error)}`);
    } finally {
      setActiveNodeOperation('');
    }
  };

  const inspectNode = async (node: NodeOption) => {
    actions.setActiveNode(node);
    await actions.refreshAll();
  };

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
      {(nodeOperationMessage || nodeOperationError) && (
        <section className={`notice ${nodeOperationError ? 'notice-error' : 'notice-success'}`} aria-live="polite">
          {nodeOperationError || nodeOperationMessage}
        </section>
      )}

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

        <section className="panel span-2 admin-node-control-panel">
          <PanelHeader title="Node Health & Sync Controls" actionLabel="Refresh nodes" onAction={refreshNodeStatuses} />
          <div className="admin-node-grid">
            {nodeStatuses.map((nodeStatus) => {
              const operationPrefix = `${nodeStatus.node.departmentCode}:`;
              const isBusy = activeNodeOperation.startsWith(operationPrefix);

              return (
                <div className="admin-node-card" key={nodeStatus.node.departmentCode}>
                  <div className="admin-node-card-header">
                    <div>
                      <strong>{nodeStatus.node.name}</strong>
                      <small>{nodeStatus.node.baseUrl}</small>
                    </div>
                    <span className={`status-pill ${nodeStatus.health === 'Online' ? 'status-pill-success' : 'status-pill-warning'}`}>{nodeStatus.health}</span>
                  </div>
                  <div className="info-grid compact">
                    <Info label="Citizens" value={nodeStatus.citizens} />
                    <Info label="Pending Outbox" value={nodeStatus.pendingOutbox} />
                    <Info label="Failed Outbox" value={nodeStatus.failedOutbox} />
                    <Info label="Pending Inbox" value={nodeStatus.pendingInbox} />
                    <Info label="Receipts" value={nodeStatus.receipts} />
                    <Info label="Last Check" value={nodeStatus.message} />
                  </div>
                  <div className="button-row admin-node-actions">
                    <Button disabled={isBusy} onClick={() => inspectNode(nodeStatus.node)}>Inspect</Button>
                    <Button disabled={isBusy} onClick={() => runNodeOperation(nodeStatus.node, 'Publish outbox', (client) => client.publishOutbox())}>Publish Outbox</Button>
                    <Button disabled={isBusy} onClick={() => runNodeOperation(nodeStatus.node, 'Apply inbox', (client) => client.applyInbox())}>Apply Inbox</Button>
                    <Button
                      className="primary-button"
                      disabled={isBusy}
                      onClick={() => runNodeOperation(nodeStatus.node, 'Full sync', async (client) => {
                        const publishResult = await client.publishOutbox();
                        const applyResult = await client.applyInbox();
                        return [publishResult, applyResult];
                      })}
                    >
                      Full Sync
                    </Button>
                  </div>
                </div>
              );
            })}
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
          <AuditPanel title="Outbox Queue" rows={state.outbox.slice(0, 8).map((entry) => [syncStatusText[entry.status] ?? 'Queued', entry.retryCount > 0 ? 'Retry scheduled' : 'Ready for delivery', 'Peer departments'])} />
        </div>
        <AuditPanel title="Inbox Queue" rows={state.inbox.slice(0, 8).map((entry) => [syncStatusText[entry.status] ?? 'Received', entry.citizenNationalIdNumber ? 'Citizen record matched' : 'Citizen pending match', entry.appliedAtUtc ? 'Applied' : 'Awaiting review'])} />
        <AuditPanel title="Sync Receipts" rows={state.receipts.slice(0, 8).map((receipt) => [receiptResultText[receipt.result] ?? 'Delivery recorded', 'Peer department', new Date(receipt.receivedAtUtc).toLocaleString()])} />
      </div>
    </main>
  );
};

export default AdminPage;
