import { useEffect } from 'react';
import { Button } from 'antd';
import { Link, useNavigate, useParams } from 'react-router-dom';
import type { DepartmentCode } from '../../api/types';
import { Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentDisplayName, departmentShortName, formatCitizenFieldValue, getCitizenFieldLabel, normalizeFieldName } from '../../utils/departmentFieldPolicy';

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
const DepartmentRequestReviewPage = ({ departmentCode, title }: DepartmentRequestReviewPageProps) => {
  const { requestId } = useParams();
  const navigate = useNavigate();
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const departmentNode = nodes.find((node) => node.departmentCode === departmentCode) ?? nodes[0];
  const request = state.changeRequests.find((item) => item.id === requestId);
  const citizen = state.citizens.find((item) => item.id === request?.citizenId);
  const citizenFields = buildCitizenFieldPolicies(citizen);
  const firstApprover = state.users[0];
  const departmentApproval = request?.approvals.find((item) => item.approvingNodeId === firstApprover?.departmentNodeId);
  const completedApprovals = request?.approvals.filter((item) => item.decision === 2).length ?? 0;
  const requiredApprovalCount = request?.fieldChanges.reduce((count, change) => {
    const matchingPolicy = citizenFields.find((field) => {
      const fieldKey = normalizeFieldName(field.key);
      const fieldLabel = normalizeFieldName(field.label);
      const changeField = normalizeFieldName(change.fieldName);

      return fieldKey === changeField ||
        fieldLabel === changeField ||
        (changeField === 'contactdetails' && (fieldKey === 'emailaddress' || fieldKey === 'phonenumber'));
    });

    return Math.max(count, matchingPolicy?.approvalDepartmentCodes.length ?? nodes.length);
  }, 0) ?? 0;
  const canRequestDepartmentApproval = Boolean(request && request.status === 1 && !departmentApproval);
  const canApproveAfterReview = Boolean(request && departmentApproval && departmentApproval.decision !== 2 && request.status !== 4 && request.status !== 5);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;
  const baseRoute = departmentRoutes[departmentCode];

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
          <p>Review the citizen record, requested field changes, approval trail, and sync impact before recording an official decision.</p>
        </div>
        <span className="trust-pill">{statusText[request.status] ?? `Status ${request.status}`}</span>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="proposal-metrics compact-metrics">
        <Metric label="Required Reviews" value={requiredApprovalCount} />
        <Metric label="Field Changes" value={request.fieldChanges.length} />
        <Metric label="Approvals Recorded" value={completedApprovals} />
        <Metric label="Record Version" value={citizen?.recordVersion ?? request.expectedCitizenVersion} />
      </section>

      <section className="request-detail-grid">
        <section className="panel">
          <h2>Citizen identity snapshot</h2>
          <div className="request-review-grid review-grid-wide">
            <div><span>Citizen</span><strong>{citizen?.displayName ?? 'Unknown citizen'}</strong></div>
            <div><span>National ID</span><strong>{citizen?.nationalIdNumber ?? 'Unknown'}</strong></div>
            <div><span>Email</span><strong>{citizen?.emailAddress ?? 'Unknown'}</strong></div>
            <div><span>Phone</span><strong>{citizen?.phoneNumber ?? 'Unknown'}</strong></div>
            <div><span>Citizen status</span><strong>{citizen?.status ?? 'Unknown'}</strong></div>
            <div><span>Created</span><strong>{formatDate(citizen?.createdAtUtc)}</strong></div>
          </div>
        </section>

        <section className="panel">
          <h2>Request summary</h2>
          <div className="request-review-grid review-grid-wide">
            <div><span>Request</span><strong>{request.fieldChanges[0] ? getCitizenFieldLabel(request.fieldChanges[0].fieldName) : 'Citizen record update'}</strong></div>
            <div><span>Reviewing department</span><strong>{departmentShortName[departmentCode]}</strong></div>
            <div><span>Reason</span><strong>{request.reason || 'No reason supplied'}</strong></div>
            <div><span>Submitted</span><strong>{formatDate(request.createdAtUtc)}</strong></div>
            <div><span>Expected version</span><strong>{request.expectedCitizenVersion}</strong></div>
            <div><span>Committed version</span><strong>{request.committedCitizenVersion ?? 'Not committed'}</strong></div>
          </div>
        </section>

        <section className="panel span-2">
          <h2>Requested field changes</h2>
          <div className="request-change-list">
            {request.fieldChanges.map((change) => {
              const changeField = normalizeFieldName(change.fieldName);
              const matchingPolicies = citizenFields.filter((field) => {
                const fieldKey = normalizeFieldName(field.key);
                const fieldLabel = normalizeFieldName(field.label);

                return fieldKey === changeField ||
                  fieldLabel === changeField ||
                  (changeField === 'contactdetails' && (fieldKey === 'emailaddress' || fieldKey === 'phonenumber'));
              });
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
          <div className="request-approval-trail">
            {request.approvals.length === 0 ? (
              <p className="empty-text">No approval has been requested from this department yet.</p>
            ) : (
              request.approvals.map((approval) => (
                <div className="approval-trail-row" key={approval.id}>
                  <span>{approval.approverDepartmentName || approval.approvingNodeId.slice(0, 8).toUpperCase()}</span>
                  <strong>{approvalDecisionText[approval.decision] ?? `Decision ${approval.decision}`}</strong>
                  <small>{approval.approverFullName || 'No named approver'} {approval.approverRole ? `- ${approval.approverRole}` : ''}</small>
                  <small>{formatDate(approval.decidedAtUtc)}</small>
                </div>
              ))
            )}
          </div>
        </section>

        <section className="panel">
          <h2>Review checklist</h2>
          <div className="review-checklist">
            <div><span>1</span><p>Confirm the citizen identity and National ID match the submitted request.</p></div>
            <div><span>2</span><p>Compare old and new values for obvious data-entry or fraud risk.</p></div>
            <div><span>3</span><p>Confirm this department is legally allowed to approve the affected field.</p></div>
            <div><span>4</span><p>Record a decision only after the request approval entry exists for this node.</p></div>
          </div>
        </section>

        <section className="panel span-2">
          <h2>Decision controls</h2>
          <div className="decision-warning">
            Approval is an official department decision. This action is written to the local node ledger flow and can later be synchronized to peer nodes.
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
