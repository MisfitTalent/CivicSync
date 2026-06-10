import { useEffect } from 'react';
import { Button, Empty } from 'antd';
import type { DepartmentCode } from '../../api/types';
import { AuditPanel, Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

interface DepartmentLedgerPageProps {
  departmentCode: DepartmentCode;
  title: string;
}

const DepartmentLedgerPage = ({ departmentCode, title }: DepartmentLedgerPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const canCommitSelectedRequest = selectedRequest?.status === 3;
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="department-proposal-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">{title} Workspace</p>
          <h2>Ledger & Sync</h2>
          <p>Inspect ledger entries, publish approved outbox events, and apply received inbox updates for this node.</p>
        </div>
        <span className="status-pill status-pill-success">Chain verified</span>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="proposal-metrics">
        <Metric label="Ledger Entries" value={state.ledger.length} />
        <Metric label="Outbox Events" value={state.outbox.length} />
        <Metric label="Inbox Entries" value={state.inbox.length} />
        <Metric label="Sync Receipts" value={state.receipts.length} />
      </section>

      <div className="proposal-dashboard-grid department-ledger-grid">
        <section className="panel ledger-history-panel">
          <div className="panel-header">
            <h2>Ledger Entries</h2>
            <span className="status-pill">{departmentNode.name}</span>
          </div>
          {state.ledger.length === 0 ? (
            <Empty className="empty-text" description="No ledger entries recorded on this node yet." />
          ) : (
            <div className="ledger-timeline">
              {state.ledger.slice(0, 12).map((entry) => (
                <article className="ledger-entry-card" key={entry.id}>
                  <div className="ledger-entry-status">
                    <span className="status-pill status-pill-success">Sequence #{entry.sequenceNumber}</span>
                    <small>{new Date(entry.createdAtUtc).toLocaleString()}</small>
                  </div>
                  <div className="ledger-entry-main">
                    <div>
                      <strong>{entry.currentProofHash.slice(0, 24)}</strong>
                      <p>Previous proof: {entry.previousProofHash.slice(0, 24)}</p>
                    </div>
                    <code>{entry.changeRequestId.slice(0, 8).toUpperCase()}</code>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <aside className="department-side-stack">
          <section className="panel">
            <h2>Approval & Sync Actions</h2>
            <div className="action-stack">
              <div className="action-context">
                <span>Selected Request</span>
                <strong>{selectedRequest ? `${selectedRequest.id.slice(0, 8)} - ${statusText[selectedRequest.status] ?? selectedRequest.status}` : 'None selected'}</strong>
              </div>
              <Button onClick={() => actions.commitRequest(selectedRequest?.id)} disabled={state.isLoading || !canCommitSelectedRequest}>Commit Ledger</Button>
              <Button onClick={actions.publishOutbox} disabled={state.isLoading}>Publish Outbox</Button>
              <Button onClick={actions.applyInbox} disabled={state.isLoading}>Apply Inbox</Button>
            </div>
          </section>

          <AuditPanel title="Outbox" rows={state.outbox.slice(0, 5).map((entry) => [statusText[entry.status] ?? entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
          <AuditPanel title="Inbox" rows={state.inbox.slice(0, 5).map((entry) => [statusText[entry.status] ?? entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
        </aside>
      </div>
    </main>
  );
};

export default DepartmentLedgerPage;

