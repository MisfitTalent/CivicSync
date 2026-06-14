import { useEffect } from 'react';
import { Button } from 'antd';
import { Link, useNavigate, useParams } from 'react-router-dom';
import type { DepartmentCode } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentDisplayName, departmentShortName, formatCitizenFieldValue, getCitizenFieldLabel, normalizeFieldName } from '../../utils/departmentFieldPolicy';
import { findDepartmentApproval } from '../../utils/departmentApprovals';

interface DepartmentRequestReviewPageProps {
  departmentCode: DepartmentCode;
  title: string;
}

const approvalDecisionText: Record<number, string> = {
  1: 'Pending',
  2: 'Approved',
  3: 'Rejected',
};

const departmentRoutes: Record<DepartmentCode, string> = {
  1: '/home-affairs',
  2: '/sars',
  3: '/municipality',
  4: '/home-affairs',
  5: '/home-affairs',
};

const formatDate = (value?: string) => (value ? new Date(value).toLocaleString() : 'Not recorded');

const getLedgerSyncMeaning = (
  hasLedgerEntry: boolean,
  requestStatus: number,
  completedApprovals: number,
  pendingApprovals: number,
  requiredApprovalCount: number,
  nextApproverName?: string,
) => {
  if (hasLedgerEntry || requestStatus === 5) {
    return 'Committed to the audit ledger. The next sync step is to publish the signed outbox event so peer departments can verify the proof hash, store the inbox event, and apply the same citizen update to their own local databases.';
  }

  if (requestStatus === 4) {
    return 'Rejected requests are not committed to the audit ledger and are not published to peer departments. The request remains visible only as review history.';
  }

  if (completedApprovals > 0) {
    const remainingApprovals = Math.max(requiredApprovalCount - completedApprovals, 0);

    return remainingApprovals > 0
      ? `${completedApprovals} approval${completedApprovals === 1 ? '' : 's'} recorded. ${remainingApprovals} more required approval${remainingApprovals === 1 ? '' : 's'} must still be recorded before the change can be committed to the ledger and synced to peer departments.`
      : 'All required approvals are recorded. The next step is to commit the approved change to the audit ledger, then publish the outbox event to peer departments.';
  }

  if (pendingApprovals > 0) {
    return `${pendingApprovals} department approval request${pendingApprovals === 1 ? '' : 's'} pending${nextApproverName ? ` with ${nextApproverName}` : ''}. The reviewer must approve or reject before the change can be committed to the ledger or published to peer departments.`;
  }

  return 'No department approval request has been opened yet. Request the relevant department approvals before recording decisions, committing the ledger entry, or publishing the update to peer departments.';
};

const getMatchingPolicies = (fieldName: string, citizenFields: ReturnType<typeof buildCitizenFieldPolicies>) => {
  const changeField = normalizeFieldName(fieldName);

  return citizenFields.filter((field) => {
    const fieldKey = normalizeFieldName(field.key);
    const fieldLabel = normalizeFieldName(field.label);

    return fieldKey === changeField ||
      fieldLabel === changeField ||
      (changeField === 'contactdetails' && (fieldKey === 'emailaddress' || fieldKey === 'phonenumber'));
  });
};

const DepartmentRequestReviewPage = ({ departmentCode, title }: DepartmentRequestReviewPageProps) => {
  const { requestId } = useParams();
  const navigate = useNavigate();
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const request = state.changeRequests.find((item) => item.id === requestId);
  const citizen = state.citizens.find((item) => item.id === request?.citizenId);
  const citizenFields = buildCitizenFieldPolicies(citizen);
  const departmentApproval = request ? findDepartmentApproval(request, departmentCode) : undefined;
  const completedApprovals = request?.approvals.filter((item) => item.decision === 2).length ?? 0;
  const pendingApprovals = request?.approvals.filter((item) => item.decision === 1).length ?? 0;
  const requiredApprovalCount = request?.fieldChanges.reduce((count, change) => {
    const matchingPolicies = getMatchingPolicies(change.fieldName, citizenFields);

    return Math.max(count, matchingPolicies.length > 0
      ? Math.max(...matchingPolicies.map((field) => field.approvalDepartmentCodes.length))
      : nodes.length);
  }, 0) ?? 0;
  const canRequestDepartmentApproval = Boolean(request && request.status === 1 && !departmentApproval);
  const canApproveAfterReview = Boolean(request && departmentApproval && departmentApproval.decision !== 2 && request.status !== 4 && request.status !== 5);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;
  const baseRoute = departmentRoutes[departmentCode];
  const latestLedgerEntry = request ? state.ledger.find((entry) => entry.changeRequestId === request.id) : undefined;
  const nextApproverName = departmentApproval?.approverFullName;
  const evidenceFiles = request?.evidenceFiles ?? [];

  useEffect(() => {
    if (state.activeNode.departmentCode !== departmentCode) {
      actions.setActiveNode(departmentNode);
    }
  }, [actions, departmentCode, departmentNode, state.activeNode.departmentCode]);

  useEffect(() => {
    if (requestId && state.selectedRequestId !== requestId) {
      actions.setSelectedRequestId(requestId);
    }
  }, [actions, requestId, state.selectedRequestId]);

  if (!request) {
    return (
      <main className="department-proposal-page compact-department-page">
        <section className="proposal-intro">
          <div>
            <Link className="back-link" to={`${baseRoute}/requests`}>Back to requests</Link>
            <h2>Request Review</h2>
            <p>{state.isLoading ? 'Loading request details from this node.' : 'The selected request is not available on this node.'}</p>
          </div>
          <span className="trust-pill">{departmentShortName[departmentCode]}</span>
        </section>
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
        <section className="panel">
          <p className="empty-text">Refresh node data or return to the request queue and select another request.</p>
          <div className="approval-actions single-action">
            <Button onClick={actions.refreshAll} disabled={state.isLoading}>Refresh</Button>
            <Button className="primary-button" onClick={() => navigate(`${baseRoute}/requests`)}>Back to request queue</Button>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="department-proposal-page compact-department-page request-detail-page">
      <section className="proposal-intro request-detail-hero">
        <div>
          <Link className="back-link" to={`${baseRoute}/requests`}>Back to requests</Link>
          <p className="eyebrow">{title} Review Dossier</p>
          <h2>{citizen?.displayName ?? 'Unknown citizen'} change request</h2>
          <p>Review the complete citizen context, field ownership, approvals, and ledger impact before recording a department decision.</p>
        </div>
        <span className="trust-pill">{statusText[request.status] ?? `Status ${request.status}`}</span>
      </section>

      {(state.isError || state.isSuccess) && (
        <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>
      )}

      <section className="proposal-metrics compact-metrics">
        <Metric label="Required Reviews" value={requiredApprovalCount} />
        <Metric label="Field Changes" value={request.fieldChanges.length} />
        <Metric label="Approvals Recorded" value={completedApprovals} />
        <Metric label="Record Version" value={citizen?.recordVersion ?? request.expectedCitizenVersion} />
      </section>

      <section className="request-detail-grid request-detail-grid-balanced">
        <section className="panel span-2 request-dossier-panel">
          <h2>Citizen dossier</h2>
          <p className="panel-helper">Full record view for decision-making. Restricted fields remain masked according to this department&apos;s POPIA access policy.</p>
          <div className="dossier-field-grid">
            {citizenFields.map((field) => {
              const canAccess = field.accessDepartmentCodes.includes(departmentCode);

              return (
                <article className={`dossier-field-card ${canAccess ? '' : 'restricted'}`} key={field.key}>
                  <span>{field.label}</span>
                  <strong>{canAccess ? field.value : 'Restricted'}</strong>
                  <small>{field.category} - owned by {departmentDisplayName[field.ownerDepartmentCode]}</small>
                </article>
              );
            })}
          </div>
        </section>

        <section className="panel request-summary-panel">
          <h2>Request summary</h2>
          <div className="request-review-grid review-grid-wide">
            <div><span>Status</span><strong>{statusText[request.status] ?? `Status ${request.status}`}</strong></div>
            <div><span>Reviewing department</span><strong>{departmentDisplayName[departmentCode]}</strong></div>
            <div><span>Reason</span><strong>{request.reason || 'No reason supplied'}</strong></div>
            <div><span>Submitted</span><strong>{formatDate(request.createdAtUtc)}</strong></div>
            <div><span>Expected version</span><strong>{request.expectedCitizenVersion}</strong></div>
            <div><span>Committed version</span><strong>{request.committedCitizenVersion ?? 'Not committed'}</strong></div>
          </div>
          <div className="request-reason-full">
            <span>Full reason supplied by requester</span>
            <p>{request.reason || 'No reason supplied'}</p>
          </div>
        </section>

        <section className="panel request-summary-panel">
          <h2>Evidence</h2>
          {evidenceFiles.length === 0 ? (
            <div className="evidence-status-card">
              <span>Stored evidence</span>
              <strong>No files attached</strong>
              <p>The requester submitted this change without supporting evidence files.</p>
            </div>
          ) : (
            <div className="evidence-file-list evidence-file-list-stacked">
              {evidenceFiles.map((file) => (
                <article className="evidence-file-card" key={file.id}>
                  <span>{file.contentType}</span>
                  <strong>{file.fileName}</strong>
                  <small>{Math.ceil(file.sizeBytes / 1024)} KB - proof hash {file.contentHash.slice(0, 12)}</small>
                  <Button
                    onClick={() => actions.downloadEvidenceFile(request.id, file.id)}
                    disabled={state.isLoading}
                  >
                    Download evidence
                  </Button>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="panel span-2">
          <h2>Requested field changes</h2>
          <div className="request-change-list">
            {request.fieldChanges.map((change) => {
              const matchingPolicies = getMatchingPolicies(change.fieldName, citizenFields);
              const ownershipText = matchingPolicies.length > 0
                ? matchingPolicies.map((field) => `${field.label}: ${departmentDisplayName[field.ownerDepartmentCode]}`).join(', ')
                : 'Ownership not mapped';
              const approverText = matchingPolicies.length > 0
                ? Array.from(new Set(matchingPolicies.flatMap((field) => field.approvalDepartmentCodes))).map((code) => departmentDisplayName[code]).join(', ')
                : 'All relevant departments';

              return (
                <article className="request-change-row request-change-row-wide" key={change.id}>
                  <div>
                    <span>Field</span>
                    <strong>{getCitizenFieldLabel(change.fieldName)}</strong>
                    <small>{ownershipText}</small>
                  </div>
                  <div>
                    <span>Old value</span>
                    <strong>{formatCitizenFieldValue(change.fieldName, change.oldValue)}</strong>
                  </div>
                  <div>
                    <span>New value</span>
                    <strong>{formatCitizenFieldValue(change.fieldName, change.newValue)}</strong>
                  </div>
                  <div>
                    <span>Required approvers</span>
                    <strong>{approverText}</strong>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <section className="panel">
          <h2>Approval trail</h2>
          <div className="request-approval-trail approval-trail-cards">
            {request.approvals.length === 0 ? (
              <p className="empty-text">No approval has been requested from this department yet.</p>
            ) : (
              request.approvals.map((approval) => {
                const matchingUser = state.users.find((user) => user.departmentNodeId === approval.approvingNodeId || user.id === approval.approverUserId);
                const departmentName = approval.approverDepartmentName || (matchingUser ? departmentDisplayName[departmentCode] : 'Department approval queue');
                const approverName = approval.approverFullName || matchingUser?.fullName;
                const approverRole = approval.approverRole || matchingUser?.role;
                const isPending = approval.decision === 1;

                return (
                  <article className="approval-trail-card" key={approval.id}>
                    <div>
                      <span>Department</span>
                      <strong>{departmentName}</strong>
                    </div>
                    <div>
                      <span>Decision</span>
                      <strong>{approvalDecisionText[approval.decision] ?? `Decision ${approval.decision}`}</strong>
                    </div>
                    <div className="approval-trail-card-wide">
                      <span>Responsible approver</span>
                      <strong>{approverName || (isPending ? 'Awaiting assigned department approver' : 'Approver not returned by API')}</strong>
                      {approverRole && <small>{approverRole}</small>}
                    </div>
                    <div>
                      <span>Recorded at</span>
                      <strong>{formatDate(approval.decidedAtUtc)}</strong>
                    </div>
                    {approval.comment && (
                      <div className="approval-trail-card-wide">
                        <span>Decision note</span>
                        <strong>{approval.comment}</strong>
                      </div>
                    )}
                  </article>
                );
              })
            )}
          </div>
        </section>

        <section className="panel">
          <h2>Ledger impact</h2>
          <div className="workspace-context-grid workspace-context-grid-wide">
            <div className="workspace-context-item">
              <span>Audit entry</span>
              <strong>{latestLedgerEntry ? `Sequence ${latestLedgerEntry.sequenceNumber}` : 'Not committed'}</strong>
            </div>
            <div className="workspace-context-item">
              <span>Proof hash</span>
              <strong>{latestLedgerEntry?.currentProofHash?.slice(0, 12) ?? 'Pending'}</strong>
            </div>
          </div>
          <div className="sync-meaning-card">
            <span>Ledger and sync status</span>
            <p>{getLedgerSyncMeaning(Boolean(latestLedgerEntry), request.status, completedApprovals, pendingApprovals, requiredApprovalCount, nextApproverName)}</p>
          </div>
        </section>

        <section className="panel span-2">
          <h2>Decision controls</h2>
          <div className="decision-warning">
            Approval is an official department decision. Confirm field ownership, evidence, and old/new values before recording it.
          </div>
          <div className="approval-actions request-detail-actions">
            <Button onClick={() => actions.requestApproval(request.id)} disabled={state.isLoading || !canRequestDepartmentApproval}>
              {departmentApproval ? 'Department approval requested' : 'Request department approval'}
            </Button>
            <Button className="primary-button" onClick={() => actions.approveRequest(request.id)} disabled={state.isLoading || !canApproveAfterReview}>
              {departmentApproval?.decision === 2 ? 'Already approved' : 'Approve after review'}
            </Button>
            <Button onClick={() => navigate(`${baseRoute}/requests`)}>Return to queue</Button>
          </div>
        </section>
      </section>
    </main>
  );
};

export default DepartmentRequestReviewPage;
