import { useEffect } from 'react';
import { Button } from 'antd';
import type { DepartmentCode } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

interface DepartmentRequestsPageProps {
  departmentCode: DepartmentCode;
  title: string;
}

const departmentShortName: Record<DepartmentCode, string> = {
  1: 'HA',
  2: 'SARS',
  3: 'MUN',
  4: 'HEALTH',
  5: 'SAFETY',
};

const DepartmentRequestsPage = ({ departmentCode, title }: DepartmentRequestsPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const firstApprover = state.users[0];
  const pendingRequests = state.changeRequests.filter((request) => request.status !== 5);
  const approvedRequests = state.changeRequests.filter((request) => request.status === 3);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
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
          <h2>Update Requests</h2>
          <p>Review citizen record changes assigned to this department node and record approval decisions.</p>
        </div>
        <span className="trust-pill">POPIA Enforced</span>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="proposal-metrics">
        <Metric label="Open Requests" value={pendingRequests.length} />
        <Metric label="Approved Requests" value={approvedRequests.length} />
        <Metric label="Department Users" value={state.users.length} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>

      <div className="proposal-dashboard-grid">
        <section className="panel">
          <div className="proposal-card-heading">
            <h2>Pending Approvals</h2>
            <span className="count-pill">{pendingRequests.length}</span>
          </div>
          <div className="approval-list officer-request-list">
            {pendingRequests.length === 0 && <p className="empty-text">No pending approvals on this node.</p>}
            {pendingRequests.map((request) => {
              const approval = request.approvals.find((item) => item.approvingNodeId === firstApprover?.departmentNodeId);
              const hasApproved = approval?.decision === 2;
              const canRequestApproval = request.status === 1 && !approval;
              const canApprove = request.status !== 4 && request.status !== 5 && !hasApproved;
              const fieldSummary = request.fieldChanges.map((change) => `${change.fieldName} -> ${change.newValue}`).join(', ');

              return (
                <article
                  className={`approval-card ${request.id === state.selectedRequestId ? 'selected' : ''}`}
                  key={request.id}
                  onClick={() => actions.setSelectedRequestId(request.id)}
                >
                  <div className="request-card-header">
                    <strong>{request.id.slice(0, 8).toUpperCase()}</strong>
                    <span className={`status-pill ${request.status === 3 ? 'status-pill-success' : 'status-pill-warning'}`}>{statusText[request.status] ?? `Status ${request.status}`}</span>
                  </div>
                  <small>{request.reason}</small>
                  <div className="approval-change">{fieldSummary || 'No field changes'}</div>
                  <div className="approval-actions">
                    <Button
                      onClick={(event) => {
                        event.stopPropagation();
                        actions.requestApproval(request.id);
                      }}
                      disabled={state.isLoading || !canRequestApproval}
                    >
                      {approval ? 'Requested' : 'Request'}
                    </Button>
                    <Button
                      className="primary-button"
                      onClick={(event) => {
                        event.stopPropagation();
                        actions.approveRequest(request.id);
                      }}
                      disabled={state.isLoading || !canApprove}
                    >
                      {hasApproved ? 'Approved' : 'Approve'}
                    </Button>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <aside className="department-side-stack">
          <section className="panel">
            <h2>Selected Request</h2>
            <div className="access-summary-list">
              <div className="access-summary-row"><span>Request</span><strong>{selectedRequest?.id.slice(0, 8).toUpperCase() ?? 'None selected'}</strong></div>
              <div className="access-summary-row"><span>Status</span><strong>{selectedRequest ? statusText[selectedRequest.status] ?? selectedRequest.status : 'None'}</strong></div>
              <div className="access-summary-row"><span>Approvals</span><strong>{selectedRequest ? `${selectedRequest.approvals.length}/${nodes.length}` : '0/0'}</strong></div>
              <div className="access-summary-row"><span>Approver</span><strong>{firstApprover ? `${firstApprover.fullName} (${firstApprover.role})` : 'No user loaded'}</strong></div>
            </div>
          </section>

          <section className="panel">
            <h2>Node Context</h2>
            <div className="access-summary-list">
              <div className="access-summary-row"><span>Department</span><strong>{departmentShortName[departmentCode]}</strong></div>
              <div className="access-summary-row"><span>API</span><strong>{state.nodeInfo?.apiBaseUrl ?? departmentNode.baseUrl}</strong></div>
              <div className="access-summary-row"><span>Peers</span><strong>{state.nodeInfo?.peers?.length ?? 0}</strong></div>
            </div>
          </section>
        </aside>
      </div>
    </main>
  );
};

export default DepartmentRequestsPage;

