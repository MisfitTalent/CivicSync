import { Button } from 'antd';
import { AuditPanel, Info, Metric, PanelHeader, TextInput } from '../../components/dashboard/DashboardWidgets';
import { statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const CitizenPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);

  return (
    <main className="page-stack">
      <section className="page-intro">
        <div>
          <p className="eyebrow">Citizen portal</p>
          <h2>Register or request a contact update</h2>
          <p>The citizen starts the workflow here. Departments approve and sync the final ledger entry from their own pages.</p>
        </div>
        <Metric label="Visible Citizens" value={state.citizens.length} />
      </section>

      <section className="notice" aria-live="polite">{state.message}</section>

      <div className="role-grid">
        <section className="panel">
          <h2>Create Citizen Profile</h2>
          <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.createCitizen(); }}>
            <TextInput label="National ID" value={state.citizenForm.nationalIdNumber} onChange={(nationalIdNumber) => actions.updateCitizenForm({ nationalIdNumber })} required />
            <TextInput label="First Name" value={state.citizenForm.firstName} onChange={(firstName) => actions.updateCitizenForm({ firstName })} required />
            <TextInput label="Last Name" value={state.citizenForm.lastName} onChange={(lastName) => actions.updateCitizenForm({ lastName })} required />
            <TextInput label="Email" type="email" value={state.citizenForm.emailAddress} onChange={(emailAddress) => actions.updateCitizenForm({ emailAddress })} required />
            <TextInput label="Phone" value={state.citizenForm.phoneNumber} onChange={(phoneNumber) => actions.updateCitizenForm({ phoneNumber })} required />
            <Button className="primary-button" htmlType="submit" disabled={state.isLoading}>Create Profile</Button>
          </form>
        </section>

        <section className="panel">
          <h2>My Citizen Record</h2>
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
          <h2>Request Change</h2>
          <div className="workflow-grid">
            <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.submitChangeRequest(); }}>
              <Info label="Selected Citizen" value={selectedCitizen?.displayName ?? 'None'} />
              <TextInput label="Reason" value={state.changeForm.reason} onChange={(reason) => actions.updateChangeForm({ reason })} required />
              <TextInput label="New Email" type="email" value={state.changeForm.newEmailAddress} onChange={(newEmailAddress) => actions.updateChangeForm({ newEmailAddress })} required />
              <TextInput label="New Phone" value={state.changeForm.newPhoneNumber} onChange={(newPhoneNumber) => actions.updateChangeForm({ newPhoneNumber })} required />
              <Button className="primary-button" htmlType="submit" disabled={state.isLoading}>Submit Change Request</Button>
            </form>
            <div className="action-stack">
              <Info label="Selected Request" value={selectedRequest ? `${selectedRequest.id.slice(0, 8)} - ${statusText[selectedRequest.status] ?? selectedRequest.status}` : 'None'} />
              <p className="helper-text">After submission, open the correct department page to approve, commit, and publish the sync.</p>
            </div>
          </div>
        </section>

        <AuditPanel title="My Requests" rows={state.changeRequests.slice(0, 8).map((request) => [statusText[request.status] ?? `Status ${request.status}`, request.reason, request.id.slice(0, 8)])} />
      </div>
    </main>
  );
};

export default CitizenPage;
