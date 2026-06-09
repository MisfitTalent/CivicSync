import { Button, Empty } from 'antd';
import { AuditPanel, Info, Metric, TextInput } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const formatDate = (value: string) => new Date(value).toLocaleString();

const CitizenPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;
  const approvedRequests = state.changeRequests.filter((request) => request.status === 3 || request.status === 5).length;
  const pendingRequests = state.changeRequests.filter((request) => request.status === 1 || request.status === 2).length;

  return (
    <main className="page-stack proposal-page">
      <section className="proposal-intro">
        <div>
          <p className="eyebrow">Citizen Portal</p>
          <h2>My Profile & Update Requests</h2>
          <p>View your linked citizen record and track change requests across all department nodes.</p>
        </div>
        <div className="proposal-status-group">
          <span className="status-pill status-pill-success">Connected to {state.activeNode.name}</span>
          <Metric label="Linked Records" value={selectedCitizen ? 1 : 0} />
        </div>
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <section className="status-strip">
        <Metric label="Active Departments" value={nodes.length} />
        <Metric label="Pending Requests" value={pendingRequests} />
        <Metric label="Approved Changes" value={approvedRequests} />
        <Metric label="Ledger Entries" value={state.ledger.length} />
      </section>

      <div className="citizen-dashboard-grid">
        <section className="panel citizen-record-panel">
          <div className="panel-header">
            <h2>My Linked Citizen Record</h2>
            {selectedCitizen && <span className="status-pill">SA-CSL-{selectedCitizen.id.slice(0, 4).toUpperCase()}</span>}
          </div>

          {selectedCitizen ? (
            <div className="citizen-field-grid">
              <Info label="Full Name" value={selectedCitizen.displayName} />
              <Info label="National ID" value={selectedCitizen.nationalIdNumber} />
              <Info label="Email Address" value={selectedCitizen.emailAddress} />
              <Info label="Phone Number" value={selectedCitizen.phoneNumber} />
              <Info label="Record Version" value={selectedCitizen.recordVersion} />
              <Info label="Created At" value={formatDate(selectedCitizen.createdAtUtc)} />
            </div>
          ) : (
            <Empty className="empty-text" description="No linked citizen record found. Ask Home Affairs or Admin to register the citizen first." />
          )}
        </section>

        <section className="panel">
          <div className="panel-header">
            <h2>Update Requests</h2>
            <Button className="primary-button" disabled={!selectedCitizen} onClick={() => document.getElementById('citizen-request-form')?.scrollIntoView({ behavior: 'smooth' })}>+ New</Button>
          </div>
          <div className="request-card-list">
            {state.changeRequests.length === 0 ? <Empty className="empty-text" description="No update requests yet." /> : state.changeRequests.slice(0, 5).map((request) => (
              <button className={`request-card ${request.id === state.selectedRequestId ? 'selected' : ''}`} key={request.id} onClick={() => actions.setSelectedRequestId(request.id)}>
                <div className="request-card-header">
                  <strong>{request.fieldChanges[0]?.fieldName ?? 'Citizen Record'}</strong>
                  <span className={`status-pill ${request.status === 3 || request.status === 5 ? 'status-pill-success' : 'status-pill-warning'}`}>{statusText[request.status] ?? `Status ${request.status}`}</span>
                </div>
                <small>{formatDate(request.createdAtUtc)}</small>
                <span>{request.reason}</span>
                <small>{request.approvals.length}/{nodes.length} departments</small>
              </button>
            ))}
          </div>
        </section>

        <section className="panel span-2" id="citizen-request-form">
          <h2>Request Contact Change</h2>
          <div className="workflow-grid">
            <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.submitChangeRequest(); }}>
              <Info label="Selected Citizen" value={selectedCitizen?.displayName ?? 'None'} />
              <TextInput label="Reason" value={state.changeForm.reason} onChange={(reason) => actions.updateChangeForm({ reason })} required />
              <TextInput label="New Email" type="email" value={state.changeForm.newEmailAddress} onChange={(newEmailAddress) => actions.updateChangeForm({ newEmailAddress })} required />
              <TextInput label="New Phone" value={state.changeForm.newPhoneNumber} onChange={(newPhoneNumber) => actions.updateChangeForm({ newPhoneNumber })} required />
              <Button className="primary-button" htmlType="submit" disabled={state.isLoading || !selectedCitizen}>Submit Change Request</Button>
            </form>
            <div className="action-stack">
              <Info label="Selected Request" value={selectedRequest ? `${selectedRequest.id.slice(0, 8)} - ${statusText[selectedRequest.status] ?? selectedRequest.status}` : 'None'} />
              <p className="helper-text">After submission, the relevant department reviews, approves, commits, and publishes the sync.</p>
            </div>
          </div>
        </section>

        <AuditPanel title="Recent Request History" rows={state.changeRequests.slice(0, 8).map((request) => [statusText[request.status] ?? `Status ${request.status}`, request.reason, request.id.slice(0, 8)])} />
      </div>
    </main>
  );
};

export default CitizenPage;
