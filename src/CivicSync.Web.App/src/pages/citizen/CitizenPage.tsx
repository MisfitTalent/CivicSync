import { Button, Empty } from 'antd';
import { AuditPanel, Info, Metric, TextInput } from '../../components/dashboard/DashboardWidgets';
import { statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const CitizenPage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;

  return (
    <main className="page-stack">
      <section className="page-intro">
        <div>
          <p className="eyebrow">Citizen portal</p>
          <h2>My profile and update requests</h2>
          <p>Citizens view their linked record and request updates. Official department users register new citizen records.</p>
        </div>
        <Metric label="Linked Records" value={selectedCitizen ? 1 : 0} />
      </section>

      <section className={noticeClassName} aria-live="polite">{noticeMessage}</section>

      <div className="role-grid">
        <section className="panel">
          <h2>My Linked Citizen Record</h2>
          {selectedCitizen ? (
            <div className="info-grid">
              <Info label="Name" value={selectedCitizen.displayName} />
              <Info label="National ID" value={selectedCitizen.nationalIdNumber} />
              <Info label="Email" value={selectedCitizen.emailAddress} />
              <Info label="Phone" value={selectedCitizen.phoneNumber} />
            </div>
          ) : (
            <Empty className="empty-text" description="No linked citizen record found. Ask Home Affairs or Admin to register the citizen first." />
          )}
        </section>

        <section className="panel">
          <h2>Select My Record</h2>
          <div className="list-scroll">
            {state.citizens.length === 0 ? <Empty className="empty-text" description="No citizen records loaded." /> : state.citizens.map((citizen) => (
              <button className={`list-item ${citizen.id === state.selectedCitizenId ? 'selected' : ''}`} key={citizen.id} onClick={() => actions.setSelectedCitizenId(citizen.id)}>
                <strong>{citizen.displayName}</strong>
                <span>{citizen.nationalIdNumber}</span>
                <small>{citizen.emailAddress}</small>
              </button>
            ))}
          </div>
        </section>

        <section className="panel span-2">
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

        <AuditPanel title="My Requests" rows={state.changeRequests.slice(0, 8).map((request) => [statusText[request.status] ?? `Status ${request.status}`, request.reason, request.id.slice(0, 8)])} />
      </div>
    </main>
  );
};

export default CitizenPage;
