import { useEffect } from 'react';
import { Button } from 'antd';
import { AuditPanel, Info, Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import type { DepartmentCode } from '../../api/types';

interface DepartmentPageProps {
  departmentCode: DepartmentCode;
  title: string;
  responsibility: string;
}

const DepartmentPage = ({ departmentCode, title, responsibility }: DepartmentPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const firstApprover = state.users[0];

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="page-stack">
      <section className="page-intro">
        <div>
          <p className="eyebrow">Department workspace</p>
          <h2>{title}</h2>
          <p>{responsibility}</p>
        </div>
        <div className="department-metrics">
          <Metric label="Requests" value={state.changeRequests.length} />
          <Metric label="Ledger" value={state.ledger.length} />
          <Metric label="Inbox" value={state.inbox.length} />
        </div>
      </section>

      <section className="notice" aria-live="polite">{state.message}</section>

      <div className="department-grid">
        <section className="panel span-2">
          <PanelHeader title="Node Overview" actionLabel="Refresh" onAction={actions.refreshAll} />
          <div className="info-grid">
            <Info label="Department Code" value={state.nodeInfo?.departmentCode ?? departmentCode} />
            <Info label="API Base URL" value={state.nodeInfo?.apiBaseUrl ?? departmentNode.baseUrl} />
            <Info label="Peer Count" value={state.nodeInfo?.peers?.length ?? 0} />
            <Info label="Default Approver" value={firstApprover ? `${firstApprover.fullName} (${firstApprover.role})` : 'No approver loaded'} />
          </div>
        </section>

        <section className="panel">
          <h2>Pending Work</h2>
          <div className="list-scroll tall">
            {state.changeRequests.map((request) => (
              <button className={`list-item ${request.id === state.selectedRequestId ? 'selected' : ''}`} key={request.id} onClick={() => actions.setSelectedRequestId(request.id)}>
                <strong>{statusText[request.status] ?? `Status ${request.status}`}</strong>
                <span>{request.reason}</span>
                <small>{request.fieldChanges.map((change) => change.fieldName).join(', ') || 'No field changes'}</small>
              </button>
            ))}
          </div>
        </section>

        <section className="panel">
          <h2>Approval & Sync Actions</h2>
          <div className="action-stack">
            <Info label="Selected Request" value={selectedRequest ? `${selectedRequest.id.slice(0, 8)} - ${statusText[selectedRequest.status] ?? selectedRequest.status}` : 'None'} />
            <Button onClick={actions.requestApproval} disabled={state.isLoading || !selectedRequest}>Request Approval</Button>
            <Button onClick={actions.approveRequest} disabled={state.isLoading || !selectedRequest}>Approve</Button>
            <Button onClick={actions.commitRequest} disabled={state.isLoading || !selectedRequest}>Commit Ledger</Button>
            <Button onClick={actions.publishOutbox} disabled={state.isLoading}>Publish Outbox</Button>
            <Button onClick={actions.applyInbox} disabled={state.isLoading}>Apply Inbox</Button>
          </div>
        </section>

        <AuditPanel title="Ledger" rows={state.ledger.slice(0, 8).map((entry) => [`#${entry.sequenceNumber}`, entry.currentProofHash.slice(0, 16), new Date(entry.createdAtUtc).toLocaleString()])} />
        <AuditPanel title="Outbox" rows={state.outbox.slice(0, 8).map((entry) => [statusText[entry.status] ?? entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Inbox" rows={state.inbox.slice(0, 8).map((entry) => [statusText[entry.status] ?? entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
      </div>
    </main>
  );
};

export default DepartmentPage;
