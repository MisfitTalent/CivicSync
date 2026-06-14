import { Button, Empty } from 'antd';
import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { nodes } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { buildCitizenFieldPolicies, departmentDisplayName } from '../../utils/departmentFieldPolicy';

type RequestStep = number;
type FieldDrafts = Record<string, { newValue: string; reason: string }>;

const readFileAsBase64 = (file: File) =>
  new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result ?? '');
      resolve(result.includes(',') ? result.split(',')[1] : result);
    };
    reader.onerror = () => reject(reader.error ?? new Error('Could not read evidence file.'));
    reader.readAsDataURL(file);
  });

const RequestUpdatePage = () => {
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const [step, setStep] = useState<RequestStep>(1);
  const [selectedFieldKeys, setSelectedFieldKeys] = useState<string[]>(['FullName']);
  const [fieldDrafts, setFieldDrafts] = useState<FieldDrafts>({});
  const [evidenceFiles, setEvidenceFiles] = useState<File[]>([]);
  const [submittedRequestId, setSubmittedRequestId] = useState('');

  const fieldOptions = useMemo(() => buildCitizenFieldPolicies(selectedCitizen), [selectedCitizen]);
  const selectedFields = selectedFieldKeys
    .map((fieldKey) => fieldOptions.find((field) => field.key === fieldKey))
    .filter((field): field is NonNullable<typeof field> => Boolean(field));
  const currentFieldIndex = step - 2;
  const currentField = selectedFields[currentFieldIndex];
  const documentsStep = selectedFields.length + 2;
  const submittedStep = selectedFields.length + 3;
  const approvalDepartmentCodes = Array.from(new Set(selectedFields.flatMap((field) => field.approvalDepartmentCodes)));
  const approvalNodes = approvalDepartmentCodes.map((departmentCode) => ({
    departmentCode,
    name: departmentDisplayName[departmentCode],
  }));
  const canContinueFromField = Boolean(selectedCitizen && selectedFields.length > 0 && selectedFields.every((field) => field.supportedByBackend));
  const currentFieldDraft = currentField ? fieldDrafts[currentField.key] ?? { newValue: '', reason: '' } : { newValue: '', reason: '' };
  const canContinueFromCurrentValue = Boolean(currentFieldDraft.newValue.trim() && currentFieldDraft.reason.trim());

  const submitRequest = async () => {
    const requestedFieldChanges = selectedFields.map((field) => ({
      fieldName: field.key,
      newValue: (fieldDrafts[field.key]?.newValue ?? '').trim(),
    }));
    const reasons = selectedFields.map((field) => `${field.label}: ${(fieldDrafts[field.key]?.reason ?? '').trim()}`);

    if (requestedFieldChanges.length === 0 || requestedFieldChanges.some((fieldChange) => !fieldChange.newValue) || reasons.some((reason) => reason.endsWith(': '))) {
      return;
    }

    try {
      const encodedEvidenceFiles = await Promise.all(evidenceFiles.map(async (file) => ({
        fileName: file.name,
        contentType: file.type || 'application/octet-stream',
        contentBase64: await readFileAsBase64(file),
      })));

      const requestId = await actions.submitFieldChangeRequest({
        fieldName: requestedFieldChanges[0].fieldName,
        newValue: requestedFieldChanges[0].newValue,
        reason: reasons.join(' | '),
        fieldChanges: requestedFieldChanges,
        evidenceFiles: encodedEvidenceFiles,
      });

      setSubmittedRequestId(requestId);
      setStep(submittedStep);
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

  const toggleSelectedField = (fieldKey: string) => {
    setSelectedFieldKeys((currentKeys) => {
      if (currentKeys.includes(fieldKey)) {
        return currentKeys.filter((key) => key !== fieldKey);
      }

      return [...currentKeys, fieldKey];
    });
  };

  const updateFieldDraft = (fieldKey: string, values: Partial<FieldDrafts[string]>) => {
    setFieldDrafts((currentDrafts) => ({
      ...currentDrafts,
      [fieldKey]: {
        ...(currentDrafts[fieldKey] ?? { newValue: '', reason: '' }),
        ...values,
      },
    }));
  };

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
        {renderStepIndicator(1, 'Select Fields')}
        {selectedFields.map((field, index) => renderStepIndicator(index + 2, field.label))}
        {renderStepIndicator(documentsStep, 'Supporting Docs')}
        {renderStepIndicator(submittedStep, 'Submitted')}
      </section>

      {step === 1 && (
        <section className="panel wizard-card">
          <h2>Which fields would you like to update?</h2>
          <p>Select one or more fields from your citizen record. Each selected field gets its own update page before submission.</p>
          <div className="wizard-field-grid">
            {fieldOptions.map((field) => (
              <button
                className={`wizard-field-card ${selectedFieldKeys.includes(field.key) ? 'selected' : ''}`}
                disabled={!field.supportedByBackend}
                key={field.key}
                onClick={() => toggleSelectedField(field.key)}
                type="button"
                title={field.helper}
              >
                <strong>{field.label}</strong>
                <small>{field.value}</small>
                <em>{field.supportedByBackend ? field.helper : `${field.helper} Backend support pending.`}</em>
              </button>
            ))}
          </div>
          {selectedFields.length > 0 && (
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

      {step > 1 && step < documentsStep && currentField && (
        <section className="panel wizard-card">
          <h2>Enter the new value</h2>
          <p>Updating {currentFieldIndex + 1} of {selectedFields.length}: <strong>{currentField.label}</strong></p>
          <label>
            <span>Current value</span>
            <input value={currentField.value} readOnly />
          </label>
          <label>
            <span>New value</span>
            <input
              value={currentFieldDraft.newValue}
              onChange={(event) => updateFieldDraft(currentField.key, { newValue: event.target.value })}
              placeholder={`Enter new ${currentField.label.toLowerCase()}...`}
            />
          </label>
          <label>
            <span>Reason for change</span>
            <textarea
              value={currentFieldDraft.reason}
              onChange={(event) => updateFieldDraft(currentField.key, { reason: event.target.value })}
              placeholder="Briefly explain why this field needs to be updated..."
            />
          </label>
          <div className="wizard-actions split">
            <Button onClick={() => setStep(step === 2 ? 1 : step - 1)}>Back</Button>
            <Button className="primary-button" disabled={!canContinueFromCurrentValue} onClick={() => setStep(step + 1)}>Continue</Button>
          </div>
        </section>
      )}

      {step === documentsStep && (
        <section className="panel wizard-card">
          <h2>Supporting documents</h2>
          <p>Upload proof to support {selectedFields.length === 1 ? 'this change request' : 'these change requests'}. Stored evidence will appear in the citizen portal and every department review dossier.</p>
          <label className="document-dropzone">
            <span>Drop your document here or browse files</span>
            <small>PDF, JPG, PNG - max 10MB</small>
            <input
              type="file"
              multiple
              onChange={(event) => setEvidenceFiles(Array.from(event.target.files ?? []))}
            />
          </label>
          {evidenceFiles.length > 0 && (
            <div className="evidence-file-list">
              {evidenceFiles.map((file) => (
                <span className="evidence-file-pill" key={`${file.name}-${file.size}`}>
                  {file.name} ({Math.ceil(file.size / 1024)} KB)
                </span>
              ))}
            </div>
          )}
          <div className="popia-warning">Your document is treated as encrypted proof. Only departments authorized to approve this field should access it.</div>
          <div className="wizard-actions split"><Button onClick={() => setStep(documentsStep - 1)}>Back</Button><Button className="primary-button" disabled={state.isLoading} onClick={submitRequest}>Submit Request</Button></div>
        </section>
      )}

      {step === submittedStep && selectedFields.length > 0 && (
        <section className="panel wizard-card submitted-card">
          <div className="submitted-icon">✓</div>
          <h2>Request Submitted</h2>
          <p>Your update request has been sent to the active department node for approval and ledger processing.</p>
          <div className="submission-summary">
            <span><small>Request Status</small><strong>{(submittedRequestId || state.selectedRequestId) ? 'Submitted for review' : 'Pending refresh'}</strong></span>
            <span><small>Fields</small><strong>{selectedFields.map((field) => field.label).join(', ')}</strong></span>
            <span><small>Changes</small><strong>{selectedFields.length}</strong></span>
            <span><small>Evidence</small><strong>{evidenceFiles.length > 0 ? `${evidenceFiles.length} file${evidenceFiles.length === 1 ? '' : 's'} stored` : 'No file attached'}</strong></span>
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
