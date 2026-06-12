import { useEffect } from 'react';
import { Button, Empty } from 'antd';
import type { DepartmentCode, SyncInboxEntry } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

interface DepartmentInboxPageProps {
  departmentCode?: DepartmentCode;
  title: string;
}

const syncStatusText: Record<number, string> = {
  1: 'Pending',
  2: 'Published',
  3: 'Received',
  4: 'Applied',
  5: 'Failed',
};

const getInboxEntryStatusClassName = (entry: SyncInboxEntry) => {
  if (entry.status === 4) {
    return 'status-pill status-pill-success';
  }

  if (entry.status === 5) {
    return 'status-pill status-pill-danger';
  }

  return 'status-pill status-pill-warning';
};

const DepartmentInboxPage = ({ departmentCode, title }: DepartmentInboxPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode);
  const receivedInboxEntries = state.inbox.filter((entry) => entry.status === 3);
  const unappliedInboxEntries = state.inbox.filter((entry) => entry.status !== 4);
  const failedInboxEntries = state.inbox.filter((entry) => entry.status === 5);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  useEffect(() => {
    if (departmentNode && state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="department-proposal-page compact-department-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">{title} Workspace</p>
          <h2>Sync Inbox</h2>
          <p>Review received peer updates, apply pending inbox entries, and inspect failed synchronization messages.</p>
        </div>
        <Button className="primary-button" onClick={actions.applyInbox} disabled={state.isLoading || unappliedInboxEntries.length === 0}>
          Apply Pending Inbox
        </Button>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="proposal-metrics compact-metrics">
        <Metric label="Inbox Entries" value={state.inbox.length} />
        <Metric label="Unread / Pending" value={unappliedInboxEntries.length} />
        <Metric label="Received" value={receivedInboxEntries.length} />
        <Metric label="Failed" value={failedInboxEntries.length} />
      </section>

      <section className="panel inbox-workspace-panel">
        <div className="panel-header">
          <h2>Inbox Messages</h2>
          <span className="status-pill">{state.activeNode.name}</span>
        </div>

        {state.inbox.length === 0 ? (
          <Empty className="empty-text" description="No inbox entries received on this node yet." />
        ) : (
          <div className="inbox-entry-list">
            {state.inbox.map((entry) => (
              <article className="inbox-entry-card" key={entry.id}>
                <div className="inbox-entry-heading">
                  <div>
                    <strong>{entry.citizenNationalIdNumber || 'Unknown citizen'}</strong>
                    <span>Citizen record synchronization</span>
                  </div>
                  <span className={getInboxEntryStatusClassName(entry)}>{syncStatusText[entry.status] ?? `Status ${entry.status}`}</span>
                </div>
                <div className="inbox-entry-body">
                  <div>
                    <span>Source</span>
                    <strong>Peer department</strong>
                  </div>
                  <div>
                    <span>Received at</span>
                    <strong>{new Date(entry.createdAtUtc).toLocaleString()}</strong>
                  </div>
                  <div>
                    <span>Applied at</span>
                    <strong>{entry.appliedAtUtc ? new Date(entry.appliedAtUtc).toLocaleString() : 'Not applied yet'}</strong>
                  </div>
                </div>
                <p className="inbox-payload-preview">{entry.fieldChangesJson ? 'Field change package received for review.' : 'No field change details supplied.'}</p>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
};

export default DepartmentInboxPage;
