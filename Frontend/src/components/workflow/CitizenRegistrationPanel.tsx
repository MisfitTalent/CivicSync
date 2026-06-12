import { Button } from 'antd';
import { TextInput } from '../dashboard/DashboardWidgets';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';

const CitizenRegistrationPanel = ({ title = 'Register Citizen Record' }: { title?: string }) => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();

  return (
    <section className="panel">
      <h2>{title}</h2>
      <p className="helper-text">Official users register citizen records before citizens can request changes against them.</p>
      <form className="form-stack" onSubmit={(event) => { event.preventDefault(); actions.createCitizen(); }}>
        <TextInput label="National ID" value={state.citizenForm.nationalIdNumber} onChange={(nationalIdNumber) => actions.updateCitizenForm({ nationalIdNumber })} required />
        <TextInput label="First Name" value={state.citizenForm.firstName} onChange={(firstName) => actions.updateCitizenForm({ firstName })} required />
        <TextInput label="Last Name" value={state.citizenForm.lastName} onChange={(lastName) => actions.updateCitizenForm({ lastName })} required />
        <TextInput label="Email" type="email" value={state.citizenForm.emailAddress} onChange={(emailAddress) => actions.updateCitizenForm({ emailAddress })} required />
        <TextInput label="Phone" value={state.citizenForm.phoneNumber} onChange={(phoneNumber) => actions.updateCitizenForm({ phoneNumber })} required />
        <Button className="primary-button" htmlType="submit" disabled={state.isLoading}>Register Citizen</Button>
      </form>
    </section>
  );
};

export default CitizenRegistrationPanel;
