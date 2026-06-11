import { useEffect } from 'react';
import { Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import type { ChangeRequest, DepartmentCode } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { formatCitizenFieldValue, getCitizenFieldLabel } from '../../utils/departmentFieldPolicy';

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

const departmentRoutes: Record<DepartmentCode, string> = {
  1: '/home-affairs',
  2: '/sars',
  3: '/municipality',
  4: '/home-affairs',
  5: '/home-affairs',
};

const requestNeedsDepartmentReview = (request: ChangeRequest, approvingNodeId?: string) => {
  const approval = request.approvals.find((item) => item.approvingNodeId === approvingNodeId);
  const requestIsOpen = request.status === 1 || request.status === 2;
  const approvalIsOpen = !approval || approval.decision === 1;

  return requestIsOpen && approvalIsOpen;
};

const requestApprovedByDepartment = (request: ChangeRequest, approvingNodeId?: string) =>
  request.approvals.some((item) => item.approvingNodeId === approvingNodeId && item.decision === 2);

const DepartmentRequestsPage = ({ departmentCode, title }: DepartmentRequestsPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const firstApprover = state.users[0];
  const requestsNeedingReview = state.changeRequests.filter((request) =>
    requestNeedsDepartmentReview(request, firstApprover?.departmentNodeId)
  );
  const approvedRequests = state.changeRequests.filter((request) =>
    requestApprovedByDepartment(request, firstApprover?.departmentNodeId)
  );
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
          <h2>Update Requests</h2>
          <p>Review citizen record changes assigned to this department node and record approval decisions.</p>
        </div>
        <span className="trust-pill">POPIA Enforced</span>
      </section>

      {(state.isError || state.isSuccess) && (
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
      )}

      <section className="proposal-metrics compact-metrics">
        <Metric label="Needs Review" value={requestsNeedingReview.length} />
        <Metric label="Approved By Department" value={approvedRequests.length} />
        <Metric label="Department Users" value={state.users.length} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>

      <div className="proposal-dashboard-grid department-request-grid">
        <section className="panel">
          <div className="proposal-card-heading">
            <h2>Review Queue</h2>
            <span className="count-pill">{requestsNeedingReview.length}</span>
          </div>
          <div className="approval-list officer-request-list">
            {requestsNeedingReview.length === 0 && <p className="empty-text">No requests currently need this department's review.</p>}
            {requestsNeedingReview.map((request) => {
              const primaryField = request.fieldChanges[0];
              const requestCitizen = state.citizens.find((citizen) => citizen.id === request.citizenId);

              return (
                <article
                  className={`approval-card ${request.id === state.selectedRequestId ? 'selected' : ''}`}
                  key={request.id}
                  onClick={() => actions.setSelectedRequestId(request.id)}
                >
                  <div className="request-card-header">
                    <strong>{primaryField ? getCitizenFieldLabel(primaryField.fieldName) : 'Citizen record update'}</strong>
                    <span className="status-pill status-pill-warning">{statusText[request.status] ?? `Status ${request.status}`}</span>
                  </div>
                  <div className="request-card-person">
                    <span>Citizen</span>
                    <strong>{requestCitizen?.displayName ?? 'Unknown citizen'}</strong>
                  </div>
                  <small>{request.reason || 'No reason supplied'}</small>
                  <div className="request-field-list">
                    {request.fieldChanges.length === 0 && <span className="empty-text">No field changes recorded.</span>}
                    {request.fieldChanges.map((change) => (
                      <div className="request-field-row" key={change.id}>
                        <span>{getCitizenFieldLabel(change.fieldName)}</span>
                        <strong>{formatCitizenFieldValue(change.fieldName, change.newValue)}</strong>
                      </div>
                    ))}
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
                      Open review
                    </Button>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <aside className="department-side-stack">
          <section className="panel">
            <h2>Review process</h2>
            <div className="review-checklist">
              <div><span>1</span><p>Select a request from the queue.</p></div>
              <div><span>2</span><p>Open the full review dossier for the citizen and request history.</p></div>
              <div><span>3</span><p>Request this department's approval record if it does not exist yet.</p></div>
              <div><span>4</span><p>Approve only after confirming the field ownership and old/new values.</p></div>
            </div>
          </section>

          <section className="panel">
            <h2>Workspace Context</h2>
            <div className="access-summary-list">
              <div className="access-summary-row"><span>Department</span><strong>{departmentShortName[departmentCode]}</strong></div>
              <div className="access-summary-row"><span>Connection</span><strong>Secure department workspace</strong></div>
              <div className="access-summary-row"><span>Peer Departments</span><strong>{state.nodeInfo?.peers?.length ?? 0}</strong></div>
            </div>
          </section>
        </aside>
      </div>
    </main>
  );
};

export default DepartmentRequestsPage;
