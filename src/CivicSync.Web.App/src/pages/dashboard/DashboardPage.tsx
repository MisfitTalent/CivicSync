import { Button } from 'antd';
import { AuditPanel, Info, Metric, PanelHeader, TextInput } from '../../components/dashboard/DashboardWidgets';
import { statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const DashboardPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const firstApprover = state.users[0];
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  return (
    <main>
      <section className="status-strip">
        <Metric label="Citizens" value={state.citizens.length} />
        <Metric label="Requests" value={state.changeRequests.length} />
        <Metric label="Ledger" value={state.ledger.length} />
        <Metric label="Outbox" value={state.outbox.length} />
        <Metric label="Inbox" value={state.inbox.length} />
        <Metric label="Receipts" value={state.receipts.length} />
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <div className="dashboard-grid">
        <section className="panel span-2">
          <PanelHeader title="Node Overview" actionLabel="Refresh" onAction={actions.refreshAll} />
          <div className="info-grid">
            <Info label="Department Code" value={state.nodeInfo?.departmentCode ?? '-'} />
            <Info label="API Base URL" value={state.nodeInfo?.apiBaseUrl ?? state.activeNode.baseUrl} />
            <Info label="Peer Count" value={state.nodeInfo?.peers?.length ?? 0} />
            <Info label="Default Approver" value={firstApprover ? `${firstApprover.fullName} (${firstApprover.role})` : 'No approver loaded'} />
          </div>
        </section>

        <section className="panel">
          <h2>Create Citizen</h2>
          <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.createCitizen(); }}>
            <TextInput label="National ID" value={state.citizenForm.nationalIdNumber} onChange={(nationalIdNumber) => actions.updateCitizenForm({ nationalIdNumber })} required />
            <TextInput label="First Name" value={state.citizenForm.firstName} onChange={(firstName) => actions.updateCitizenForm({ firstName })} required />
            <TextInput label="Last Name" value={state.citizenForm.lastName} onChange={(lastName) => actions.updateCitizenForm({ lastName })} required />
            <TextInput label="Email" type="email" value={state.citizenForm.emailAddress} onChange={(emailAddress) => actions.updateCitizenForm({ emailAddress })} required />
            <TextInput label="Phone" value={state.citizenForm.phoneNumber} onChange={(phoneNumber) => actions.updateCitizenForm({ phoneNumber })} required />
            <Button className="primary-button" htmlType="submit" disabled={state.isLoading}>Create</Button>
          </form>
        </section>

        <section className="panel">
          <h2>Citizens</h2>
          <div className="list-scroll">
            {state.citizens.map((citizen) => (
              <button className={`list-item ${citizen.id === state.selectedCitizenId ? 'selected' : ''}`} key={citizen.id} onClick={() => actions.setSelectedCitizenId(citizen.id)}>
                <strong>{citizen.displayName}</strong>
                <span>{citizen.nationalIdNumber}</span>
                <small>{citizen.emailAddress}</small>
              </button>
            ))}
          </div>
        </section>

        <section className="panel span-2">
          <h2>Change Workflow</h2>
          <div className="workflow-grid">
            <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.submitChangeRequest(); }}>
              <Info label="Selected Citizen" value={selectedCitizen?.displayName ?? 'None'} />
              <TextInput label="Reason" value={state.changeForm.reason} onChange={(reason) => actions.updateChangeForm({ reason })} required />
              <TextInput label="New Email" type="email" value={state.changeForm.newEmailAddress} onChange={(newEmailAddress) => actions.updateChangeForm({ newEmailAddress })} required />
              <TextInput label="New Phone" value={state.changeForm.newPhoneNumber} onChange={(newPhoneNumber) => actions.updateChangeForm({ newPhoneNumber })} required />
              <Button className="primary-button" htmlType="submit" disabled={state.isLoading}>Submit Change</Button>
            </form>

            <div className="action-stack">
              <Info label="Selected Request" value={selectedRequest ? `${selectedRequest.id.slice(0, 8)} - ${statusText[selectedRequest.status] ?? selectedRequest.status}` : 'None'} />
              <Button onClick={() => actions.requestApproval()} disabled={state.isLoading || !selectedRequest}>Request Approval</Button>
              <Button onClick={() => actions.approveRequest()} disabled={state.isLoading || !selectedRequest}>Approve</Button>
              <Button onClick={() => actions.commitRequest()} disabled={state.isLoading || !selectedRequest}>Commit Ledger</Button>
              <Button onClick={actions.publishOutbox} disabled={state.isLoading}>Publish Outbox</Button>
              <Button onClick={actions.applyInbox} disabled={state.isLoading}>Apply Inbox</Button>
            </div>
          </div>
        </section>

        <section className="panel">
          <h2>Change Requests</h2>
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

        <AuditPanel title="Ledger" rows={state.ledger.slice(0, 6).map((entry) => [`#${entry.sequenceNumber}`, entry.currentProofHash.slice(0, 16), new Date(entry.createdAtUtc).toLocaleString()])} />
        <AuditPanel title="Outbox" rows={state.outbox.slice(0, 6).map((entry) => [entry.status.toString(), `Retries ${entry.retryCount}`, entry.ledgerEntryId.slice(0, 8)])} />
        <AuditPanel title="Inbox" rows={state.inbox.slice(0, 6).map((entry) => [entry.status.toString(), entry.citizenNationalIdNumber || 'Unknown citizen', entry.ledgerEntryId.slice(0, 8)])} />
      </div>
    </main>
  );
};

export default DashboardPage;
