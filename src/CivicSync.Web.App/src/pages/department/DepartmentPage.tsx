import { useEffect, useMemo } from 'react';
import { Button, Input } from 'antd';
import { useNavigate } from 'react-router-dom';
import { AuditPanel, Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import CitizenRegistrationPanel from '../../components/workflow/CitizenRegistrationPanel';
import type { DepartmentCode } from '../../api/types';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentShortName } from '../../utils/departmentFieldPolicy';

interface DepartmentPageProps {
  departmentCode: DepartmentCode;
  title: string;
  responsibility: string;
}

const departmentAccent: Record<DepartmentCode, string> = {
  1: 'lime',
  2: 'orange',
  3: 'blue',
  4: 'lime',
  5: 'orange',
};

const departmentRoutes: Record<DepartmentCode, string> = {
  1: '/home-affairs',
  2: '/sars',
  3: '/municipality',
  4: '/home-affairs',
  5: '/home-affairs',
};

const DepartmentPage = ({ departmentCode, title, responsibility }: DepartmentPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId) ?? state.citizens[0];
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const firstApprover = state.users[0];
  const canRegisterCitizens = departmentCode === 1;
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  const citizenFields = useMemo(() => buildCitizenFieldPolicies(selectedCitizen), [selectedCitizen]);
  const accessibleFields = citizenFields.filter((field) => field.accessDepartmentCodes.includes(departmentCode));
  const restrictedFields = citizenFields.filter((field) => !field.accessDepartmentCodes.includes(departmentCode));
  const pendingRequests = state.changeRequests.filter((request) => request.status !== 5).slice(0, 4);
  const canCommitSelectedRequest = selectedRequest?.status === 3;

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="department-proposal-page compact-department-page">
      <section className="proposal-intro">
        <div>
          <h2>Department Dashboard</h2>
          <p>{responsibility}</p>
        </div>
        <span className="trust-pill">POPIA Enforced</span>
      </section>

      <section className="department-switcher" aria-label="Department views">
        {nodes.map((node) => (
          <button
            className={`department-chip ${node.departmentCode === departmentCode ? 'active' : ''}`}
            key={node.departmentCode}
            type="button"
            aria-current={node.departmentCode === departmentCode ? 'page' : undefined}
            title="Use the matching department login to open this workspace."
          >
            <span className={`status-dot ${departmentAccent[node.departmentCode]}`} />
            {node.name}
          </button>
        ))}
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="proposal-metrics compact-metrics">
        <Metric label="Accessible Fields" value={accessibleFields.length} />
        <Metric label="Restricted Fields" value={restrictedFields.length} />
        <Metric label="Pending Requests" value={pendingRequests.length} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>
      <div className="department-workspace-grid">
        <div className="department-workspace-stack">
          <section className="panel proposal-record-card">
            <PanelHeader title="Citizen Record Viewer" actionLabel="Refresh" onAction={actions.refreshAll} />
            <Input placeholder="Search by name or ID number..." aria-label="Search citizen records" />

            <div className="popia-warning">
              Fields marked restricted are not accessible to {title} under the current POPIA field policy.
            </div>

            <div className="field-card-grid department-field-grid">
              {citizenFields.map((field) => {
                const canAccess = field.accessDepartmentCodes.includes(departmentCode);
                return (
                  <article className={`field-card ${canAccess ? '' : 'restricted'}`} key={field.key} title={field.helper}>
                    <span>{field.label}</span>
                    <strong>{canAccess ? field.value : 'Restricted'}</strong>
                    <small>{canAccess ? `${field.category} - owned by ${departmentShortName[field.ownerDepartmentCode]}` : 'POPIA restricted'}</small>
                  </article>
                );
              })}
            </div>
          </section>

          {canRegisterCitizens && <CitizenRegistrationPanel />}
          <AuditPanel title="Outbox" rows={state.outbox.slice(0, 5).map((entry) => [statusText[entry.status] ?? entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
          <AuditPanel title="Inbox" rows={state.inbox.slice(0, 5).map((entry) => [statusText[entry.status] ?? entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
        </div>

        <aside className="department-workspace-stack department-side-stack">
          <section className="panel" id="approvals">
            <div className="proposal-card-heading">
              <h2>Pending Approvals</h2>
              <span className="count-pill">{pendingRequests.length}</span>
            </div>
            <div className="approval-list">
              {pendingRequests.length === 0 && <p className="empty-text">No pending approvals on this node.</p>}
              {pendingRequests.map((request) => {
                const approval = request.approvals.find((item) => item.approvingNodeId === firstApprover?.departmentNodeId);
                const hasApproved = approval?.decision === 2;

                return (
                  <article
                    className={`approval-card ${request.id === state.selectedRequestId ? 'selected' : ''}`}
                    key={request.id}
                    onClick={() => actions.setSelectedRequestId(request.id)}
                  >
                    <strong>{selectedCitizen?.displayName ?? 'Citizen record'}</strong>
                    <small>{statusText[request.status] ?? `Status ${request.status}`}</small>
                    <div className="approval-change">
                      {request.fieldChanges.map((change) => `${change.fieldName} -> ${change.newValue}`).join(', ')}
                    </div>
                    <div className="approval-actions">
                      <Button
                        className="primary-button"
                        onClick={(event) => {
                          event.stopPropagation();
                          actions.setSelectedRequestId(request.id);
                          navigate(`${departmentRoutes[departmentCode]}/requests/${request.id}`);
                        }}
                        disabled={state.isLoading}
                      >
                        {hasApproved ? 'Review approved' : 'Review details'}
                      </Button>
                    </div>
                  </article>
                );
              })}
            </div>
          </section>

          <section className="panel department-access-summary">
            <div className="proposal-card-heading">
              <h2>Access Summary</h2>
              <span className="count-pill">{accessibleFields.length}/{citizenFields.length}</span>
            </div>
            <div className="access-summary-list">
              {citizenFields.map((field) => {
                const canAccess = field.accessDepartmentCodes.includes(departmentCode);
                return (
                  <div className="access-summary-row" key={field.key}>
                    <span>{field.label}</span>
                    <strong className={canAccess ? 'accessible' : 'restricted'}>{canAccess ? 'Accessible' : 'Restricted'}</strong>
                  </div>
                );
              })}
            </div>
          </section>

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

          <div id="ledger">
            <AuditPanel title="Ledger" rows={state.ledger.slice(0, 5).map((entry) => [`#${entry.sequenceNumber}`, entry.currentProofHash.slice(0, 16), new Date(entry.createdAtUtc).toLocaleString()])} />
          </div>

          <section className="panel department-node-context">
            <h2>Node Context</h2>
            <div className="access-summary-list">
              <div className="access-summary-row"><span>Department</span><strong>{departmentShortName[departmentCode]}</strong></div>
              <div className="access-summary-row"><span>API</span><strong>{state.nodeInfo?.apiBaseUrl ?? departmentNode.baseUrl}</strong></div>
              <div className="access-summary-row"><span>Peers</span><strong>{state.nodeInfo?.peers?.length ?? 0}</strong></div>
              <div className="access-summary-row"><span>Approver</span><strong>{firstApprover ? `${firstApprover.fullName} (${firstApprover.role})` : 'No user loaded'}</strong></div>
            </div>
          </section>
        </aside>
      </div>
    </main>
  );
};

export default DepartmentPage;
