import { useEffect, useRef, useState } from 'react';
import { Button, Empty, Input } from 'antd';
import { useNavigate } from 'react-router-dom';
import { Info, Metric } from '../../components/dashboard/DashboardWidgets';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { biometricCitizenLinkStorageKey } from '../../providers/authProvider/context';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { formatCitizenFieldValue, getCitizenFieldLabel } from '../../utils/departmentFieldPolicy';
import {
  describeFaceCapture,
  encodeFaceEmbedding,
  FACE_MODEL_NAME,
  getBiometricEnrollmentStatus,
  getDisplayBiometricReference,
  startFaceCamera,
  stopFaceCamera,
} from '../../utils/faceRecognition';

const formatDate = (value: string) => new Date(value).toLocaleString();

const rememberBiometricCitizenLink = (accountId: string | undefined, citizenId: string | undefined) => {
  if (!accountId || !citizenId) {
    return;
  }

  const storedLinks = JSON.parse(window.localStorage.getItem(biometricCitizenLinkStorageKey) || '{}') as Record<string, string>;
  window.localStorage.setItem(biometricCitizenLinkStorageKey, JSON.stringify({
    ...storedLinks,
    [accountId]: citizenId,
  }));
};

const CitizenPage = () => {
  const authState = useAuthState();
  const authActions = useAuthActions();
  const state = useCivicSyncState();
  const actions = useCivicSyncActions();
  const navigate = useNavigate();
  const selectedCitizen = state.citizens.find((citizen) => citizen.id === state.selectedCitizenId);
  const selectedRequest = state.changeRequests.find((request) => request.id === state.selectedRequestId);
  const noticeClassName = `notice ${state.isError ? 'notice-error' : state.isSuccess ? 'notice-success' : ''}`;
  const noticeMessage = state.errorMessage || state.successMessage || state.message;
  const approvedRequests = state.changeRequests.filter((request) => request.status === 3 || request.status === 5).length;
  const pendingRequests = state.changeRequests.filter((request) => request.status === 1 || request.status === 2).length;
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [biometricStream, setBiometricStream] = useState<MediaStream | null>(null);
  const [biometricStatus, setBiometricStatus] = useState('');
  const [biometricError, setBiometricError] = useState('');
  const [biometricModelStatus, setBiometricModelStatus] = useState(`${FACE_MODEL_NAME} ready.`);
  const [enrollmentPassword, setEnrollmentPassword] = useState('');

  useEffect(() => () => {
    biometricStream?.getTracks().forEach((track) => track.stop());
  }, [biometricStream]);

  const startCamera = async () => {
    setBiometricError('');
    setBiometricStatus('Starting camera...');

    let stream: MediaStream | null = null;

    try {
      if (!videoRef.current) {
        throw new Error('Camera preview is not ready.');
      }

      stream = await startFaceCamera(videoRef.current);
      setBiometricStream(stream);
      setBiometricStatus('Camera ready. Keep your face centered, then enroll or verify.');
    } catch (error) {
      stopFaceCamera(stream, videoRef.current);
      setBiometricStream(null);
      setBiometricStatus('');
      setBiometricError(error instanceof Error ? error.message : 'Camera permission was denied or the camera is unavailable.');
    }
  };

  const stopCamera = () => {
    stopFaceCamera(biometricStream, videoRef.current);
    setBiometricStream(null);
  };

  const captureFaceDescriptor = async () => {
    if (!selectedCitizen) {
      throw new Error('Select or register a citizen before using biometrics.');
    }

    if (!videoRef.current || !biometricStream) {
      throw new Error('Start the camera before capturing biometrics.');
    }

    return encodeFaceEmbedding(videoRef.current);
  };

  const handleEnrollBiometric = async () => {
    try {
      setBiometricError('');
      if (!authActions.verifyCurrentPassword(authState.currentUser?.id, enrollmentPassword)) {
        throw new Error('Enter your current account password before enrolling a face.');
      }

      setBiometricStatus('Loading local face model and capturing live face embedding...');
      const capture = await captureFaceDescriptor();
      await actions.enrollBiometric(capture.descriptor);
      rememberBiometricCitizenLink(authState.currentUser?.id, selectedCitizen?.id);
      setEnrollmentPassword('');
      setBiometricModelStatus(describeFaceCapture(capture));
      setBiometricStatus('Face biometric enrolled for this citizen.');
    } catch (error) {
      setBiometricStatus('');
      setBiometricError(error instanceof Error ? error.message : 'Biometric enrollment failed.');
    }
  };

  const handleVerifyBiometric = async () => {
    try {
      setBiometricError('');
      setBiometricStatus('Loading local face model and verifying live face embedding...');
      const capture = await captureFaceDescriptor();
      await actions.verifyBiometric(capture.descriptor);
      rememberBiometricCitizenLink(authState.currentUser?.id, selectedCitizen?.id);
      setBiometricModelStatus(describeFaceCapture(capture));
      setBiometricStatus('Face verification passed.');
    } catch (error) {
      setBiometricStatus('');
      setBiometricError(error instanceof Error ? error.message : 'Biometric verification failed.');
    }
  };

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
        <div className="citizen-main-column">
          <section className="panel citizen-record-panel">
            <div className="panel-header">
              <h2>My Linked Citizen Record</h2>
              {selectedCitizen && <span className="status-pill">Verified profile</span>}
            </div>

            {selectedCitizen ? (
              <div className="citizen-field-grid">
                <Info label="Full Name" value={selectedCitizen.displayName} />
                <Info label="National ID" value={selectedCitizen.nationalIdNumber} />
                <Info label="Email Address" value={selectedCitizen.emailAddress} />
                <Info label="Phone Number" value={selectedCitizen.phoneNumber} />
                <Info label="Record Status" value="Active department record" />
                <Info label="Registered On" value={formatDate(selectedCitizen.createdAtUtc)} />
              </div>
            ) : (
              <Empty className="empty-text" description="No linked citizen record found. Ask Home Affairs or Admin to register the citizen first." />
            )}
          </section>

          <section className="panel compact-action-panel" id="citizen-request-form">
            <div className="panel-header">
              <h2>Request Record Update</h2>
              <Button className="primary-button" disabled={!selectedCitizen} onClick={() => navigate('/citizen/request-update')}>Start Request</Button>
            </div>
            <div className="request-context-grid">
              <Info label="Selected Citizen" value={selectedCitizen?.displayName ?? 'None'} />
              <Info label="Selected Request" value={selectedRequest ? `${selectedRequest.fieldChanges[0] ? getCitizenFieldLabel(selectedRequest.fieldChanges[0].fieldName) : 'Citizen record'} - ${statusText[selectedRequest.status] ?? 'In progress'}` : 'None'} />
            </div>
            <p className="helper-text">Use the guided request flow to choose a supported citizen field, provide the new value, and submit it for department approval.</p>
          </section>

          <section className="panel compact-action-panel">
            <div className="panel-header">
              <h2>Biometric Login</h2>
              <span className={`status-pill ${selectedCitizen?.biometricReference ? 'status-pill-success' : 'status-pill-warning'}`}>
                {selectedCitizen?.biometricReference ? 'Enrolled' : 'Not enrolled'}
              </span>
            </div>
            <div className="request-context-grid">
              <Info label="Enrollment Status" value={getBiometricEnrollmentStatus(selectedCitizen?.biometricReference)} />
              <Info label="Registered Reference" value={getDisplayBiometricReference(selectedCitizen?.biometricReference)} />
            </div>
            <label className="biometric-password-confirm">
              <span>Confirm password to enroll face</span>
              <Input.Password value={enrollmentPassword} onChange={(event) => setEnrollmentPassword(event.target.value)} />
            </label>
                <div className="biometric-camera-panel">
                  <video
                    ref={videoRef}
                    className={`biometric-video ${biometricStream ? '' : 'biometric-video-idle'}`}
                    autoPlay
                    playsInline
                    muted
                    aria-label="Live camera preview for face biometric capture"
                  />
                  {!biometricStream && (
                    <div className="biometric-placeholder">Camera not started. Start the camera and allow browser permission.</div>
                  )}
            </div>
            <div className="button-row biometric-action-row">
              <Button disabled={!selectedCitizen || state.isPending || Boolean(biometricStream)} onClick={startCamera}>Start camera</Button>
              <Button disabled={!selectedCitizen || state.isPending || !biometricStream || !enrollmentPassword} onClick={handleEnrollBiometric}>Enroll face</Button>
              <Button className="primary-button" disabled={!selectedCitizen || state.isPending || !selectedCitizen?.biometricReference || !biometricStream} onClick={handleVerifyBiometric}>Verify face</Button>
              <Button disabled={!biometricStream} onClick={stopCamera}>Stop camera</Button>
            </div>
            {biometricStatus && <p className="biometric-status-text">{biometricStatus}</p>}
            {biometricError && <p className="biometric-error-text">{biometricError}</p>}
            <p className="helper-text">{biometricModelStatus}</p>
            <p className="helper-text">CivicSync stores an encoded 128D face embedding and a short reference fingerprint. It does not store the camera image.</p>
          </section>
        </div>

        <div className="citizen-side-column">
          <section className="panel" id="update-requests">
            <div className="panel-header">
              <h2>Update Requests</h2>
              <Button className="primary-button" disabled={!selectedCitizen} onClick={() => navigate('/citizen/request-update')}>+ New</Button>
            </div>
            <div className="request-card-list">
              {state.changeRequests.length === 0 ? <Empty className="empty-text" description="No update requests yet." /> : state.changeRequests.slice(0, 5).map((request) => {
                const fieldChange = request.fieldChanges[0];

                return (
                  <button className={`request-card ${request.id === state.selectedRequestId ? 'selected' : ''}`} key={request.id} onClick={() => actions.setSelectedRequestId(request.id)}>
                    <div className="request-card-header">
                      <strong>{fieldChange ? getCitizenFieldLabel(fieldChange.fieldName) : 'Citizen record'}</strong>
                      <span className={`status-pill ${request.status === 3 || request.status === 5 ? 'status-pill-success' : 'status-pill-warning'}`}>{statusText[request.status] ?? `Status ${request.status}`}</span>
                    </div>
                    <small>{formatDate(request.createdAtUtc)}</small>
                    <span>{request.reason || 'No reason supplied'}</span>
                    <small>
                      {fieldChange
                        ? `${formatCitizenFieldValue(fieldChange.fieldName, fieldChange.oldValue)} -> ${formatCitizenFieldValue(fieldChange.fieldName, fieldChange.newValue)}`
                        : 'No field change recorded'}
                    </small>
                    <small>{request.approvals.length}/{nodes.length} departments</small>
                    <small>{(request.evidenceFiles?.length ?? 0) > 0 ? `${request.evidenceFiles.length} evidence file${request.evidenceFiles.length === 1 ? '' : 's'} stored` : 'No evidence attached'}</small>
                  </button>
                );
              })}
            </div>
          </section>

          <section className="panel department-sync-panel">
            <h2>Department Sync</h2>
            <div className="sync-list">
              {nodes.map((node) => (
                <div className="sync-row" key={node.departmentCode}>
                  <span><i className={`status-dot ${node.departmentCode === 2 ? 'orange' : node.departmentCode === 3 ? 'blue' : ''}`} /> {node.name}</span>
                  <strong>{node.departmentCode === state.activeNode.departmentCode ? 'Active node' : 'Peer node'}</strong>
                </div>
              ))}
            </div>
          </section>
        </div>
      </div>
    </main>
  );
};

export default CitizenPage;

