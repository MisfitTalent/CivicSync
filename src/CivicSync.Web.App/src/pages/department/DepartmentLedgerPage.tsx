import { useEffect } from 'react';
import { Button, Empty } from 'antd';
import type { DepartmentCode } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { getCitizenFieldLabel } from '../../utils/departmentFieldPolicy';

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
    <main className="department-proposal-page compact-department-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">{title} Workspace</p>
          <h2>Ledger & Sync</h2>
          <p>
            The ledger is this node&apos;s official, tamper-evident history of approved citizen record changes. It
            keeps the audit trail and provides the proof used when syncing peer departments.
          </p>
        </div>
        <span className="status-pill status-pill-success">Chain verified</span>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="ledger-explainer-grid" aria-label="How the ledger works">
        <article className="ledger-explainer-card">
          <span>1</span>
          <div>
            <strong>Approved changes are recorded here</strong>
            <p>Once a request is approved, it becomes a ledger entry instead of silently overwriting history.</p>
          </div>
        </article>
        <article className="ledger-explainer-card">
          <span>2</span>
          <div>
            <strong>Each entry proves the previous entry</strong>
            <p>Proof hashes link entries together, so edited or missing history becomes detectable.</p>
          </div>
        </article>
        <article className="ledger-explainer-card">
          <span>3</span>
          <div>
            <strong>Ledger entries drive department sync</strong>
            <p>Publishing turns committed entries into outbox events that peer departments receive through inbox.</p>
          </div>
        </article>
      </section>

      <section className="proposal-metrics compact-metrics">
        <Metric label="Ledger Entries" value={state.ledger.length} />
        <Metric label="Outbox Events" value={state.outbox.length} />
        <Metric label="Inbox Entries" value={state.inbox.length} />
        <Metric label="Sync Receipts" value={state.receipts.length} />
      </section>

      <div className="proposal-dashboard-grid department-ledger-grid compact-ledger-grid">
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
                    <span className="status-pill status-pill-success">Entry {entry.sequenceNumber}</span>
                    <small>{new Date(entry.createdAtUtc).toLocaleString()}</small>
                  </div>
                  <div className="ledger-entry-main">
                    <div>
                      <strong>Approved change committed to ledger</strong>
                      <p>This entry locks the change into the audit chain and can be published to peer departments.</p>
                    </div>
                    <span className="status-pill">Recorded</span>
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
                <strong>{selectedRequest ? `${selectedRequest.fieldChanges[0] ? getCitizenFieldLabel(selectedRequest.fieldChanges[0].fieldName) : 'Citizen record'} - ${statusText[selectedRequest.status] ?? 'In progress'}` : 'None selected'}</strong>
              </div>
              <p className="action-helper">
                Commit creates the ledger entry. Publish sends it to peers. Apply processes received peer updates.
              </p>
              <Button onClick={() => actions.commitRequest(selectedRequest?.id)} disabled={state.isLoading || !canCommitSelectedRequest}>Commit Ledger</Button>
              <Button onClick={actions.publishOutbox} disabled={state.isLoading}>Publish Outbox</Button>
              <Button onClick={actions.applyInbox} disabled={state.isLoading}>Apply Inbox</Button>
            </div>
          </section>

        </aside>
      </div>
    </main>
  );
};

export default DepartmentLedgerPage;
