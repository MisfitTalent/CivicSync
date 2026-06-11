import { useEffect, useMemo } from 'react';
import { Button, Input } from 'antd';
import { useNavigate } from 'react-router-dom';
import { Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import CitizenRegistrationPanel from '../../components/workflow/CitizenRegistrationPanel';
import type { ChangeRequest, DepartmentCode } from '../../api/types';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentDisplayName, departmentShortName, getCitizenFieldLabel, normalizeFieldName } from '../../utils/departmentFieldPolicy';
import { requestNeedsDepartmentReview } from '../../utils/departmentApprovals';

interface DepartmentPageProps {
  departmentCode: DepartmentCode;
  title: string;
  responsibility: string;
}

const departmentRoutes: Record<DepartmentCode, string> = {
  1: '/home-affairs',
  2: '/sars',
  3: '/municipality',
  4: '/home-affairs',
  5: '/home-affairs',
};

const buildRequestTitle = (request: ChangeRequest) => {
  if (request.fieldChanges.length === 0) {
    return 'Citizen record update';
  }

  return request.fieldChanges.map((fieldChange) => getCitizenFieldLabel(fieldChange.fieldName)).join(', ');
};

const DepartmentPage = ({ departmentCode, title, responsibility }: DepartmentPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId) ?? state.citizens[0];
  const canRegisterCitizens = departmentCode === 1;
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  const citizenFields = useMemo(() => buildCitizenFieldPolicies(selectedCitizen), [selectedCitizen]);
  const accessibleFields = citizenFields.filter((field) => field.accessDepartmentCodes.includes(departmentCode));
  const restrictedFields = citizenFields.filter((field) => !field.accessDepartmentCodes.includes(departmentCode));
  const requestsNeedingReview = state.changeRequests.filter((request) =>
    requestNeedsDepartmentReview(request, departmentCode)
  );
  const reviewQueue = requestsNeedingReview.slice(0, 5);
  const latestLedgerEntry = state.ledger[0];

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  return (
    <main className="department-proposal-page compact-department-page">
      <section className="proposal-intro department-hero-compact">
        <div>
          <p className="eyebrow">{title} Workspace</p>
          <h2>Department Dashboard</h2>
          <p>{responsibility}</p>
        </div>
        <span className="trust-pill">POPIA Enforced</span>
      </section>

      {(state.isError || state.isSuccess) && (
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
      )}

      <section className="proposal-metrics compact-metrics">
        <Metric label="Accessible Fields" value={accessibleFields.length} />
        <Metric label="Restricted Fields" value={restrictedFields.length} />
        <Metric label="Needs Review" value={requestsNeedingReview.length} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>

      <div className="department-dashboard-grid">
        <section className="panel proposal-record-card department-record-panel">
          <PanelHeader title="Citizen Record Viewer" actionLabel="Refresh" onAction={actions.refreshAll} />
          <Input placeholder="Search by name or ID number..." aria-label="Search citizen records" />

          <div className="popia-warning">
            Restricted fields are hidden under the current POPIA field policy.
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

        <aside className="department-review-rail">
          <section className="panel review-queue-panel" id="approvals">
            <div className="proposal-card-heading">
              <h2>Review Queue</h2>
              <span className="count-pill">{requestsNeedingReview.length}</span>
            </div>
            <div className="approval-list review-queue-list">
              {reviewQueue.length === 0 && <p className="empty-text">No requests currently need this department&apos;s review.</p>}
              {reviewQueue.map((request) => {
                const requestCitizen = state.citizens.find((citizen) => citizen.id === request.citizenId);
                const affectedApprovers = Array.from(new Set(
                  request.fieldChanges.flatMap((fieldChange) => {
                    const changedFieldName = normalizeFieldName(fieldChange.fieldName);
                    const matchingFields = citizenFields.filter((field) => {
                      const fieldKey = normalizeFieldName(field.key);
                      const fieldLabel = normalizeFieldName(field.label);

                      return fieldKey === changedFieldName ||
                        fieldLabel === changedFieldName ||
                        (changedFieldName === 'contactdetails' && (fieldKey === 'emailaddress' || fieldKey === 'phonenumber'));
                    });

                    return matchingFields.flatMap((field) => field.approvalDepartmentCodes);
                  })
                ));

                return (
                  <article
                    className={`approval-card ${request.id === state.selectedRequestId ? 'selected' : ''}`}
                    key={request.id}
                    onClick={() => actions.setSelectedRequestId(request.id)}
                  >
                    <div className="request-card-header">
                      <strong>{buildRequestTitle(request)}</strong>
                      <span className="compact-request-status">{statusText[request.status] ?? `Status ${request.status}`}</span>
                    </div>
                    <div className="request-card-person">
                      <span>Citizen</span>
                      <strong>{requestCitizen?.displayName ?? selectedCitizen?.displayName ?? 'Citizen record'}</strong>
                    </div>
                    <small className="request-card-reason" title={request.reason || 'No reason supplied'}>{request.reason || 'No reason supplied'}</small>
                    <div className="compact-request-meta">
                      <span>{request.fieldChanges.length} field {request.fieldChanges.length === 1 ? 'change' : 'changes'}</span>
                      <span>{affectedApprovers.length > 0 ? affectedApprovers.map((code) => departmentShortName[code]).join(', ') : 'Approval mapping pending'}</span>
                    </div>
                    <Button
                      className="primary-button"
                      onClick={(event) => {
                        event.stopPropagation();
                        actions.setSelectedRequestId(request.id);
                        navigate(`${departmentRoutes[departmentCode]}/requests/${request.id}`);
                      }}
                      disabled={state.isLoading}
                    >
                      Open full review
                    </Button>
                  </article>
                );
              })}
            </div>
          </section>

          <section className="panel department-action-panel">
            <h2>Workspace Actions</h2>
            <div className="workspace-action-grid">
              <Button onClick={() => navigate(`${departmentRoutes[departmentCode]}/requests`)}>All Requests</Button>
              <Button onClick={() => navigate(`${departmentRoutes[departmentCode]}/inbox`)}>Sync Inbox</Button>
              <Button onClick={() => navigate(`${departmentRoutes[departmentCode]}/sync`)}>Sync Operations</Button>
              <Button onClick={() => navigate(`${departmentRoutes[departmentCode]}/ledger`)}>Ledger</Button>
            </div>
            <div className="workspace-context-grid workspace-context-grid-wide">
              <div className="workspace-context-item">
                <span>Department</span>
                <strong>{departmentDisplayName[departmentCode]}</strong>
              </div>
              <div className="workspace-context-item">
                <span>Peer departments</span>
                <strong>{state.nodeInfo?.peers?.length ?? 0}</strong>
              </div>
              <div className="workspace-context-item workspace-context-wide">
                <span>Latest ledger position</span>
                <strong>{latestLedgerEntry ? `Sequence ${latestLedgerEntry.sequenceNumber}` : 'No ledger entries yet'}</strong>
                <small>{latestLedgerEntry ? new Date(latestLedgerEntry.createdAtUtc).toLocaleString() : 'Commit an approved request to create a ledger entry.'}</small>
              </div>
            </div>
          </section>
        </aside>
      </div>

      {canRegisterCitizens && <CitizenRegistrationPanel />}
    </main>
  );
};

export default DepartmentPage;
