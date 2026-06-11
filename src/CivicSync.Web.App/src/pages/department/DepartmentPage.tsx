import { useEffect, useMemo, useState } from 'react';
import { Button, Input } from 'antd';
import { useNavigate } from 'react-router-dom';
import { Metric, PanelHeader } from '../../components/dashboard/DashboardWidgets';
import CitizenRegistrationPanel from '../../components/workflow/CitizenRegistrationPanel';
import type { ChangeRequest, DepartmentCode } from '../../api/types';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentShortName, getCitizenFieldLabel } from '../../utils/departmentFieldPolicy';

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

const requestNeedsDepartmentReview = (request: ChangeRequest, approvingNodeId?: string) => {
  const approval = request.approvals.find((item) => item.approvingNodeId === approvingNodeId);
  const requestIsOpen = request.status === 1 || request.status === 2;
  const approvalIsOpen = !approval || approval.decision === 1;

  return requestIsOpen && approvalIsOpen;
};

const DepartmentPage = ({ departmentCode, title, responsibility }: DepartmentPageProps) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const [expandedLedgerEntryId, setExpandedLedgerEntryId] = useState<string>();
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
  const requestsNeedingReview = state.changeRequests.filter((request) =>
    requestNeedsDepartmentReview(request, firstApprover?.departmentNodeId)
  );
  const pendingRequests = requestsNeedingReview.slice(0, 4);
  const canCommitSelectedRequest = selectedRequest?.status === 3;
  const pendingOutboxCount = state.outbox.filter((event) => event.status !== 2).length;
  const awaitingInboxCount = state.inbox.filter((entry) => !entry.appliedAtUtc).length;
  const latestLedgerSequence = state.ledger[0]?.sequenceNumber ?? 0;
  const latestLedgerDate = state.ledger[0]
    ? new Date(state.ledger[0].createdAtUtc).toLocaleString()
    : 'No ledger entries yet';
  const activeDepartmentUsers = state.users.filter((user) => user.isActive).length;
  const ledgerPreviewEntries = state.ledger.slice(0, 5).map((entry) => {
    const ledgerRequest = state.changeRequests.find((request) => request.id === entry.changeRequestId);
    const fieldNames = ledgerRequest?.fieldChanges.map((fieldChange) => getCitizenFieldLabel(fieldChange.fieldName)) ?? [];
    const requestCitizen = ledgerRequest
      ? state.citizens.find((citizen) => citizen.id === ledgerRequest.citizenId)
      : undefined;
    const outboxEvent = state.outbox.find((event) => event.ledgerEntryId === entry.id);
    const syncState = outboxEvent ? statusText[outboxEvent.status] ?? 'Queued for peers' : 'Not published yet';

    return {
      entry,
      fieldSummary: fieldNames.length > 0 ? fieldNames.join(', ') : 'Citizen record',
      citizenName: requestCitizen?.displayName ?? 'Citizen record',
      requestStatus: ledgerRequest ? statusText[ledgerRequest.status] ?? 'Committed' : 'Committed',
      requestReason: ledgerRequest?.reason || 'No request reason captured.',
      fieldCount: ledgerRequest?.fieldChanges.length ?? 0,
      syncState,
    };
  });

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

      {(state.isError || state.isSuccess) && (
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
      )}

      <section className="proposal-metrics compact-metrics">
        <Metric label="Accessible Fields" value={accessibleFields.length} />
        <Metric label="Restricted Fields" value={restrictedFields.length} />
        <Metric label="Needs Review" value={requestsNeedingReview.length} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>
      <div className="department-workspace-grid">
        <div className="department-workspace-stack">
          <section className="panel proposal-record-card">
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

          {canRegisterCitizens && <CitizenRegistrationPanel />}
          {!canRegisterCitizens && (
            <>
              <section className="panel ledger-preview-panel" id="ledger">
                <div className="proposal-card-heading">
                  <h2>Ledger</h2>
                  <span className="count-pill">{state.ledger.length}</span>
                </div>
                <div className="ledger-preview-list ledger-preview-list-wide">
                  {ledgerPreviewEntries.length === 0 && <p className="empty-text">No ledger entries recorded yet.</p>}
                  {ledgerPreviewEntries.map((item) => {
                    const isExpanded = expandedLedgerEntryId === item.entry.id;

                    return (
                      <article className="ledger-preview-card" key={item.entry.id}>
                        <div className="ledger-preview-heading">
                          <strong>Sequence {item.entry.sequenceNumber}</strong>
                          <span>{item.fieldSummary}</span>
                        </div>
                        <p>{item.requestStatus} change locked into the audit chain.</p>
                        <small>{item.syncState} - {new Date(item.entry.createdAtUtc).toLocaleString()}</small>
                        {isExpanded && (
                          <div className="ledger-preview-detail">
                            <div>
                              <span>Citizen</span>
                              <strong>{item.citizenName}</strong>
                            </div>
                            <div>
                              <span>Changed fields</span>
                              <strong>{item.fieldSummary}</strong>
                            </div>
                            <div>
                              <span>Request reason</span>
                              <strong>{item.requestReason}</strong>
                            </div>
                            <div>
                              <span>Sync meaning</span>
                              <strong>
                                {item.fieldCount > 0
                                  ? `${item.fieldCount} field ${item.fieldCount === 1 ? 'change is' : 'changes are'} ready for peer verification.`
                                  : 'Citizen record proof is ready for peer verification.'}
                              </strong>
                            </div>
                          </div>
                        )}
                        <Button
                          className="ledger-more-button"
                          type="link"
                          onClick={() => setExpandedLedgerEntryId(isExpanded ? undefined : item.entry.id)}
                        >
                          {isExpanded ? 'Show less' : 'More details'}
                        </Button>
                      </article>
                    );
                  })}
                </div>
              </section>

              <section className="panel department-node-context">
                <h2>Workspace Context</h2>
                <div className="workspace-context-grid workspace-context-grid-wide">
                  <div className="workspace-context-item">
                    <span>Department</span>
                    <strong>{departmentShortName[departmentCode]}</strong>
                  </div>
                  <div className="workspace-context-item">
                    <span>Peer departments</span>
                    <strong>{state.nodeInfo?.peers?.length ?? 0}</strong>
                  </div>
                  <div className="workspace-context-item">
                    <span>Accessible fields</span>
                    <strong>{accessibleFields.length}/{citizenFields.length}</strong>
                  </div>
                  <div className="workspace-context-item">
                    <span>Needs review</span>
                    <strong>{requestsNeedingReview.length}</strong>
                  </div>
                  <div className="workspace-context-item">
                    <span>Outbox pending</span>
                    <strong>{pendingOutboxCount}</strong>
                  </div>
                  <div className="workspace-context-item">
                    <span>Inbox awaiting</span>
                    <strong>{awaitingInboxCount}</strong>
                  </div>
                  <div className="workspace-context-item workspace-context-wide">
                    <span>Connection</span>
                    <strong>Secure department workspace</strong>
                    <small>Signs approvals, records ledger entries, and exchanges sync events with peer departments.</small>
                  </div>
                  <div className="workspace-context-item workspace-context-wide">
                    <span>Latest ledger position</span>
                    <strong>{latestLedgerSequence > 0 ? `Sequence ${latestLedgerSequence}` : 'No ledger entries yet'}</strong>
                    <small>{latestLedgerDate}</small>
                  </div>
                </div>
                {firstApprover && (
                  <div className="workspace-approver">
                    <span>Current approver</span>
                    <strong>{firstApprover.fullName}</strong>
                    <small>{firstApprover.role}</small>
                  </div>
                )}
              </section>
            </>
          )}
        </div>

        <aside className="department-workspace-stack department-side-stack">
          <section className="panel" id="approvals">
            <div className="proposal-card-heading">
              <h2>Review Queue</h2>
              <span className="count-pill">{requestsNeedingReview.length}</span>
            </div>
            <div className="approval-list">
              {pendingRequests.length === 0 && <p className="empty-text">No requests currently need this department's review.</p>}
              {pendingRequests.map((request) => {
                const requestCitizen = state.citizens.find((citizen) => citizen.id === request.citizenId);
                const primaryField = request.fieldChanges[0];

                return (
                  <article
                    className={`approval-card ${request.id === state.selectedRequestId ? 'selected' : ''}`}
                    key={request.id}
                    onClick={() => actions.setSelectedRequestId(request.id)}
                  >
                    <div className="request-card-header">
                      <strong>{primaryField ? getCitizenFieldLabel(primaryField.fieldName) : 'Citizen record update'}</strong>
                    </div>
                    <span className="compact-request-status">{statusText[request.status] ?? `Status ${request.status}`}</span>
                    <div className="request-card-person">
                      <span>Citizen</span>
                      <strong>{requestCitizen?.displayName ?? selectedCitizen?.displayName ?? 'Citizen record'}</strong>
                    </div>
                    <small className="request-card-reason">{request.reason || 'No reason supplied'}</small>
                    <div className="compact-request-meta">
                      <span>{request.fieldChanges.length} field {request.fieldChanges.length === 1 ? 'change' : 'changes'}</span>
                      <span>Full dossier required</span>
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

          <section className="panel">
            <h2>Approval & Sync Actions</h2>
            <div className="action-stack">
              <div className="action-context">
                <span>Selected Request</span>
                <strong>{selectedRequest ? `${selectedRequest.fieldChanges[0] ? getCitizenFieldLabel(selectedRequest.fieldChanges[0].fieldName) : 'Citizen record'} - ${statusText[selectedRequest.status] ?? 'In progress'}` : 'None selected'}</strong>
              </div>
              <Button onClick={() => actions.commitRequest(selectedRequest?.id)} disabled={state.isLoading || !canCommitSelectedRequest}>Commit Ledger</Button>
              <Button onClick={actions.publishOutbox} disabled={state.isLoading}>Publish Outbox</Button>
              <Button onClick={actions.applyInbox} disabled={state.isLoading}>Apply Inbox</Button>
            </div>
          </section>

          {canRegisterCitizens && (
            <section className="panel ledger-preview-panel" id="ledger">
              <div className="proposal-card-heading">
                <h2>Ledger</h2>
                <span className="count-pill">{state.ledger.length}</span>
              </div>
              <div className="ledger-preview-list">
                {ledgerPreviewEntries.length === 0 && <p className="empty-text">No ledger entries recorded yet.</p>}
                {ledgerPreviewEntries.map((item) => {
                  const isExpanded = expandedLedgerEntryId === item.entry.id;

                  return (
                    <article className="ledger-preview-card" key={item.entry.id}>
                      <div className="ledger-preview-heading">
                        <strong>Sequence {item.entry.sequenceNumber}</strong>
                        <span>{item.fieldSummary}</span>
                      </div>
                      <p>{item.requestStatus} change locked into the audit chain.</p>
                      <small>{item.syncState} - {new Date(item.entry.createdAtUtc).toLocaleString()}</small>
                      {isExpanded && (
                        <div className="ledger-preview-detail">
                          <div>
                            <span>Citizen</span>
                            <strong>{item.citizenName}</strong>
                          </div>
                          <div>
                            <span>Changed fields</span>
                            <strong>{item.fieldSummary}</strong>
                          </div>
                          <div>
                            <span>Request reason</span>
                            <strong>{item.requestReason}</strong>
                          </div>
                          <div>
                            <span>Sync meaning</span>
                            <strong>
                              {item.fieldCount > 0
                                ? `${item.fieldCount} field ${item.fieldCount === 1 ? 'change is' : 'changes are'} ready for peer verification.`
                                : 'Citizen record proof is ready for peer verification.'}
                            </strong>
                          </div>
                        </div>
                      )}
                      <Button
                        className="ledger-more-button"
                        type="link"
                        onClick={() => setExpandedLedgerEntryId(isExpanded ? undefined : item.entry.id)}
                      >
                        {isExpanded ? 'Show less' : 'More details'}
                      </Button>
                    </article>
                  );
                })}
              </div>
            </section>
          )}

        {canRegisterCitizens && <section className="panel department-node-context">
          <h2>Workspace Context</h2>
          <div className="workspace-context-grid">
            <div className="workspace-context-item">
              <span>Department</span>
              <strong>{departmentShortName[departmentCode]}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Peer departments</span>
              <strong>{state.nodeInfo?.peers?.length ?? 0}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Accessible fields</span>
              <strong>{accessibleFields.length}/{citizenFields.length}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Needs review</span>
              <strong>{requestsNeedingReview.length}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Outbox pending</span>
              <strong>{pendingOutboxCount}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Inbox awaiting</span>
              <strong>{awaitingInboxCount}</strong>
            </div>
            <div className="workspace-context-item workspace-context-wide">
              <span>Connection</span>
              <strong>Secure department workspace</strong>
              <small>Signs approvals, records ledger entries, and exchanges sync events with peer departments.</small>
            </div>
            <div className="workspace-context-item workspace-context-wide">
              <span>Latest ledger position</span>
              <strong>{latestLedgerSequence > 0 ? `Sequence ${latestLedgerSequence}` : 'No ledger entries yet'}</strong>
              <small>{latestLedgerDate}</small>
            </div>
            <div className="workspace-context-item workspace-context-wide">
              <span>Active department users</span>
              <strong>{activeDepartmentUsers}</strong>
              <small>Users loaded for this department workspace.</small>
            </div>
          </div>
          {firstApprover && (
            <div className="workspace-approver">
              <span>Current approver</span>
              <strong>{firstApprover.fullName}</strong>
              <small>{firstApprover.role}</small>
            </div>
          )}
        </section>}
        </aside>
      </div>
    </main>
  );
};

export default DepartmentPage;
