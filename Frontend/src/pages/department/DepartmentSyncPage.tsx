import { useEffect } from 'react';
import { Button, Empty } from 'antd';
import type { DepartmentCode, SyncInboxEntry, SyncOutboxEvent } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { departmentShortName } from '../../utils/departmentFieldPolicy';

interface DepartmentSyncPageProps {
  departmentCode: DepartmentCode;
  title: string;
}

const syncStatusText: Record<number, string> = {
  1: 'Waiting to publish',
  2: 'Published to peers',
  3: 'Received from peer',
  4: 'Applied locally',
  5: 'Delivery failed',
};

const getOutboxStatusClassName = (entry: SyncOutboxEvent) => {
  if (entry.status === 2 || entry.status === 4) {
    return 'status-pill status-pill-success';
  }

  if (entry.status === 5) {
    return 'status-pill status-pill-danger';
  }

  return 'status-pill status-pill-warning';
};

const getInboxStatusClassName = (entry: SyncInboxEntry) => {
  if (entry.status === 4) {
    return 'status-pill status-pill-success';
  }

  if (entry.status === 5) {
    return 'status-pill status-pill-danger';
  }

  return 'status-pill status-pill-warning';
};

const DepartmentSyncPage = ({ departmentCode, title }: DepartmentSyncPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const pendingOutboxEvents = state.outbox.filter((event) => event.status !== 2 && event.status !== 4);
  const awaitingInboxEntries = state.inbox.filter((entry) => entry.status !== 4);
  const failedSyncItems = [...state.outbox.filter((event) => event.status === 5), ...state.inbox.filter((entry) => entry.status === 5)];
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="department-proposal-page compact-department-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">{title} Workspace</p>
          <h2>Sync Operations</h2>
          <p>Track outgoing ledger publications, incoming peer updates, delivery receipts, and node health without cluttering the citizen review workspace.</p>
        </div>
        <div className="sync-action-row">
          <Button onClick={actions.publishOutbox} disabled={state.isLoading || state.outbox.length === 0}>Publish Outbox</Button>
          <Button className="primary-button" onClick={actions.applyInbox} disabled={state.isLoading || awaitingInboxEntries.length === 0}>Apply Inbox</Button>
        </div>
      </section>

      {(state.isError || state.isSuccess) && (
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
      )}

      <section className="proposal-metrics compact-metrics">
        <Metric label="Outbound Pending" value={pendingOutboxEvents.length} />
        <Metric label="Inbox Awaiting" value={awaitingInboxEntries.length} />
        <Metric label="Sync Receipts" value={state.receipts.length} />
        <Metric label="Failed Sync" value={failedSyncItems.length} />
      </section>

      <div className="sync-operations-grid">
        <section className="panel sync-panel">
          <div className="panel-header">
            <h2>Outbound Delivery Queue</h2>
            <span className="count-pill">{state.outbox.length}</span>
          </div>
          <p className="panel-helper">Ledger entries waiting to be published or confirmed by peer departments.</p>
          {state.outbox.length === 0 ? (
            <Empty className="empty-text" description="No outbound sync events for this node yet." />
          ) : (
            <div className="sync-event-list">
              {state.outbox.slice(0, 8).map((entry) => (
                <article className="sync-event-card" key={entry.id}>
                  <div className="sync-event-heading">
                    <strong>Ledger publication</strong>
                    <span className={getOutboxStatusClassName(entry)}>{syncStatusText[entry.status] ?? 'Queued'}</span>
                  </div>
                  <div className="sync-event-grid">
                    <div>
                      <span>Delivery target</span>
                      <strong>Peer departments</strong>
                    </div>
                    <div>
                      <span>Retry count</span>
                      <strong>{entry.retryCount}</strong>
                    </div>
                    <div>
                      <span>Created</span>
                      <strong>{new Date(entry.createdAtUtc).toLocaleString()}</strong>
                    </div>
                    <div>
                      <span>Last updated</span>
                      <strong>{entry.updatedAtUtc ? new Date(entry.updatedAtUtc).toLocaleString() : 'Not updated yet'}</strong>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="panel sync-panel">
          <div className="panel-header">
            <h2>Incoming Delivery Queue</h2>
            <span className="count-pill">{state.inbox.length}</span>
          </div>
          <p className="panel-helper">Peer updates received by this node before they are applied to the local citizen record copy.</p>
          {state.inbox.length === 0 ? (
            <Empty className="empty-text" description="No incoming sync events for this node yet." />
          ) : (
            <div className="sync-event-list">
              {state.inbox.slice(0, 8).map((entry) => (
                <article className="sync-event-card" key={entry.id}>
                  <div className="sync-event-heading">
                    <strong>{entry.citizenNationalIdNumber || 'Citizen pending match'}</strong>
                    <span className={getInboxStatusClassName(entry)}>{syncStatusText[entry.status] ?? 'Received'}</span>
                  </div>
                  <div className="sync-event-grid">
                    <div>
                      <span>Package</span>
                      <strong>{entry.fieldChangesJson ? 'Field changes received' : 'No field changes supplied'}</strong>
                    </div>
                    <div>
                      <span>Applied</span>
                      <strong>{entry.appliedAtUtc ? new Date(entry.appliedAtUtc).toLocaleString() : 'Not applied yet'}</strong>
                    </div>
                    <div>
                      <span>Received</span>
                      <strong>{new Date(entry.createdAtUtc).toLocaleString()}</strong>
                    </div>
                    <div>
                      <span>Last updated</span>
                      <strong>{entry.updatedAtUtc ? new Date(entry.updatedAtUtc).toLocaleString() : 'Not updated yet'}</strong>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="panel sync-panel sync-context-panel">
          <div className="panel-header">
            <h2>Sync Context</h2>
            <span className="status-pill">{departmentShortName[departmentCode]}</span>
          </div>
          <div className="workspace-context-grid">
            <div className="workspace-context-item">
              <span>Local node</span>
              <strong>{departmentNode.name}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Peer departments</span>
              <strong>{state.nodeInfo?.peers?.length ?? 0}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Ledger entries</span>
              <strong>{state.ledger.length}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Receipts</span>
              <strong>{state.receipts.length}</strong>
            </div>
            <div className="workspace-context-item workspace-context-wide">
              <span>Operational meaning</span>
              <strong>Keep this department&apos;s local copy aligned with peer nodes.</strong>
              <small>Publish sends approved ledger entries outward. Apply Inbox writes received peer changes into this node after validation.</small>
            </div>
          </div>
        </section>
      </div>
    </main>
  );
};

export default DepartmentSyncPage;
