import { Button, Empty } from 'antd';
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { nodes } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentDisplayName } from '../../utils/departmentFieldPolicy';

type RequestStep = 1 | 2 | 3 | 4;

const RequestUpdatePage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const [step, setStep] = useState<RequestStep>(1);
  const [selectedFieldKey, setSelectedFieldKey] = useState('FullName');
  const [newValue, setNewValue] = useState('');
  const [reason, setReason] = useState('');
  const [documentName, setDocumentName] = useState('');
  const [submittedRequestId, setSubmittedRequestId] = useState('');

  const fieldOptions = useMemo(() => buildCitizenFieldPolicies(selectedCitizen), [selectedCitizen]);
  const selectedField = fieldOptions.find((field) => field.key === selectedFieldKey) ?? fieldOptions[0];
  const approvalNodes = selectedField?.approvalDepartmentCodes.map((departmentCode) => ({
    departmentCode,
    name: departmentDisplayName[departmentCode],
  })) ?? [];
  const canContinueFromField = Boolean(selectedCitizen && selectedField?.supportedByBackend);
  const canContinueFromValue = Boolean(newValue.trim() && reason.trim());

  const submitRequest = async () => {
    if (!selectedField || !canContinueFromValue) {
      return;
    }

    try {
      const requestId = await actions.submitFieldChangeRequest({
        fieldName: selectedField.key,
        newValue,
        reason,
      });

      setSubmittedRequestId(requestId);
      setStep(4);
    } catch {
      return;
    }
  };

  const renderStepIndicator = (item: RequestStep, label: string) => (
    <div className={`wizard-step ${step === item ? 'active' : step > item ? 'complete' : ''}`}>
      <span>{step > item ? '✓' : item}</span>
      <small>{label}</small>
    </div>
  );

  if (!selectedCitizen) {
    return (
      <main className="page-stack proposal-page request-wizard-page">
        <div className="wizard-back-row"><Link to="/citizen">Back to Portal</Link><h2>Request Record Update</h2></div>
        <section className="panel wizard-card"><Empty description="No linked citizen record found. Ask Home Affairs or Admin to register the citizen first." /></section>
      </main>
    );
  }

  return (
    <main className="page-stack proposal-page request-wizard-page">
      <div className="wizard-back-row">
        <Link to="/citizen">Back to Portal</Link>
        <h2>Request Record Update</h2>
      </div>

      <section className="wizard-progress" aria-label="Request progress">
        {renderStepIndicator(1, 'Select Field')}
        {renderStepIndicator(2, 'New Value')}
        {renderStepIndicator(3, 'Supporting Docs')}
        {renderStepIndicator(4, 'Submitted')}
      </section>

      {step === 1 && (
        <section className="panel wizard-card">
          <h2>Which field would you like to update?</h2>
          <p>Select a field from your citizen record. Each change routes to the department that owns or approves that data.</p>
          <div className="wizard-field-grid">
            {fieldOptions.map((field) => (
              <button
                className={`wizard-field-card ${selectedFieldKey === field.key ? 'selected' : ''}`}
                disabled={!field.supportedByBackend}
                key={field.key}
                onClick={() => setSelectedFieldKey(field.key)}
                type="button"
                title={field.helper}
              >
                <strong>{field.label}</strong>
                <small>{field.value}</small>
                <em>{field.supportedByBackend ? field.helper : `${field.helper} Backend support pending.`}</em>
              </button>
            ))}
          </div>
          {selectedField && (
            <div className="approval-requirements">
              <strong>Requires approval from:</strong>
              <div>
                {approvalNodes.map((node) => (
                  <span className="department-mini-pill" key={node.departmentCode}>
                    <i className={`status-dot ${node.departmentCode === 2 ? 'orange' : node.departmentCode === 3 ? 'blue' : ''}`} />
                    {node.name}
                  </span>
                ))}
              </div>
            </div>
          )}
          <div className="wizard-actions"><Button className="primary-button" disabled={!canContinueFromField} onClick={() => setStep(2)}>Continue</Button></div>
        </section>
      )}

      {step === 2 && selectedField && (
        <section className="panel wizard-card">
          <h2>Enter the new value</h2>
          <p>Updating: <strong>{selectedField.label}</strong></p>
          <label>
            <span>Current value</span>
            <input value={selectedField.value} readOnly />
          </label>
          <label>
            <span>New value</span>
            <input value={newValue} onChange={(event) => setNewValue(event.target.value)} placeholder={`Enter new ${selectedField.label.toLowerCase()}...`} />
          </label>
          <label>
            <span>Reason for change</span>
            <textarea value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Briefly explain why this field needs to be updated..." />
          </label>
          <div className="wizard-actions split"><Button onClick={() => setStep(1)}>Back</Button><Button className="primary-button" disabled={!canContinueFromValue} onClick={() => setStep(3)}>Continue</Button></div>
        </section>
      )}

      {step === 3 && (
        <section className="panel wizard-card">
          <h2>Supporting documents</h2>
          <p>Upload proof to support your change request. Document storage is UI-only for this prototype.</p>
          <label className="document-dropzone">
            <span>Drop your document here or browse files</span>
            <small>PDF, JPG, PNG - max 10MB</small>
            <input type="file" onChange={(event) => setDocumentName(event.target.files?.[0]?.name ?? '')} />
          </label>
          {documentName && <p className="helper-text">Selected document: {documentName}</p>}
          <div className="popia-warning">Your document is treated as encrypted proof. Only departments authorized to approve this field should access it.</div>
          <div className="wizard-actions split"><Button onClick={() => setStep(2)}>Back</Button><Button className="primary-button" disabled={state.isLoading} onClick={submitRequest}>Submit Request</Button></div>
        </section>
      )}

      {step === 4 && selectedField && (
        <section className="panel wizard-card submitted-card">
          <div className="submitted-icon">✓</div>
          <h2>Request Submitted</h2>
          <p>Your update request has been sent to the active department node for approval and ledger processing.</p>
          <div className="submission-summary">
            <span><small>Request Status</small><strong>{(submittedRequestId || state.selectedRequestId) ? 'Submitted for review' : 'Pending refresh'}</strong></span>
            <span><small>Field</small><strong>{selectedField.label}</strong></span>
            <span><small>New Value</small><strong>{newValue}</strong></span>
            <span><small>Expected</small><strong>After department approval</strong></span>
          </div>
          <div className="approval-list compact">
            {approvalNodes.map((node) => <div className="approval-wait-row" key={node.departmentCode}><span><i className={`status-dot ${node.departmentCode === 2 ? 'orange' : node.departmentCode === 3 ? 'blue' : ''}`} />{node.name}</span><strong>Awaiting review</strong></div>)}
          </div>
          <Button className="primary-button full-width" onClick={() => navigate('/citizen')}>Back to Portal</Button>
        </section>
      )}
    </main>
  );
};

export default RequestUpdatePage;
