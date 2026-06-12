import { useEffect, useRef, useState } from 'react';
import { Button, Empty } from 'antd';
import { useNavigate } from 'react-router-dom';
import type * as FaceApi from '@vladmandic/face-api';
import { Info, Metric } from '../../components/dashboard/DashboardWidgets';
import { nodes, statusText } from '../../providers/civicSyncProvider/context';
import { useCivicSyncActions, useCivicSyncState } from '../../providers/civicSyncProvider';
import { formatCitizenFieldValue, getCitizenFieldLabel } from '../../utils/departmentFieldPolicy';

const FACE_MODEL_NAME = 'FaceAPI TinyFaceDetector + 68 landmarks + 128D recognition model';
const FACE_DESCRIPTOR_VERSION = 'face-api-recognition-v1';
const FACE_MODEL_URL = '/models/face-api';
const DETECTION_INPUT_SIZE = 320;
const DETECTION_SCORE_THRESHOLD = 0.55;
const REQUIRED_FACE_SAMPLES = 6;
const MAX_FACE_SAMPLE_ATTEMPTS = 12;
const FACE_SAMPLE_INTERVAL_MS = 180;
const MIN_LIVENESS_SCORE = 0.65;

let faceApi: typeof FaceApi | null = null;
let faceModelLoadPromise: Promise<void> | null = null;

type FaceCaptureResult = {
  descriptor: string;
  modelName: string;
  qualityScore: number;
  livenessScore: number;
  sampleCount: number;
};

type FaceSample = {
  descriptor: number[];
  centerX: number;
  centerY: number;
  boxWidth: number;
  boxHeight: number;
  eyeAspectRatio: number;
};

const getDisplayBiometricReference = (reference?: string) => {
  if (!reference) {
    return 'Unavailable';
  }

  return reference.split('|')[0] ?? reference;
};

const loadFaceModels = async () => {
  if (!faceModelLoadPromise) {
    faceModelLoadPromise = import('@vladmandic/face-api').then(async (module) => {
      faceApi = module;

      await Promise.all([
        faceApi.nets.tinyFaceDetector.loadFromUri(FACE_MODEL_URL),
        faceApi.nets.faceLandmark68Net.loadFromUri(FACE_MODEL_URL),
        faceApi.nets.faceRecognitionNet.loadFromUri(FACE_MODEL_URL),
      ]);
    });
  }

  await faceModelLoadPromise;
};

const wait = (milliseconds: number) => new Promise((resolve) => {
  window.setTimeout(resolve, milliseconds);
});

const encodeDescriptor = (descriptor: number[]) => {
  const bytes = new Uint8Array(new Float32Array(descriptor).buffer);
  let binary = '';

  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });

  return `${FACE_DESCRIPTOR_VERSION}:${btoa(binary)}`;
};

const averageDescriptors = (descriptors: number[][]) => {
  const totals = Array.from({ length: descriptors[0].length }, () => 0);

  descriptors.forEach((descriptor) => {
    descriptor.forEach((value, index) => {
      totals[index] += value;
    });
  });

  return totals.map((value) => Number((value / descriptors.length).toFixed(8)));
};

const pointDistance = (first: FaceApi.Point, second: FaceApi.Point) => (
  Math.hypot(first.x - second.x, first.y - second.y)
);

const calculateEyeAspectRatio = (eye: FaceApi.Point[]) => {
  if (eye.length < 6) {
    return 0;
  }

  const verticalA = pointDistance(eye[1], eye[5]);
  const verticalB = pointDistance(eye[2], eye[4]);
  const horizontal = pointDistance(eye[0], eye[3]);

  return horizontal === 0 ? 0 : (verticalA + verticalB) / (2 * horizontal);
};

const valueRange = (values: number[]) => Math.max(...values) - Math.min(...values);

const clamp = (value: number) => Math.min(1, Math.max(0, value));

const calculateLivenessScore = (samples: FaceSample[]) => {
  const firstSample = samples[0];
  const movementRange = Math.max(
    valueRange(samples.map((sample) => sample.centerX)) / firstSample.boxWidth,
    valueRange(samples.map((sample) => sample.centerY)) / firstSample.boxHeight,
  );
  const eyeRange = valueRange(samples.map((sample) => sample.eyeAspectRatio));
  const movementScore = clamp(movementRange / 0.035);
  const eyeScore = clamp(eyeRange / 0.045);

  return Number(Math.max(movementScore, eyeScore).toFixed(2));
};

const captureFaceSample = async (video: HTMLVideoElement): Promise<FaceSample | null> => {
  if (!faceApi) {
    throw new Error('Face recognition model is not loaded.');
  }

  const detection = await faceApi
    .detectSingleFace(video, new faceApi.TinyFaceDetectorOptions({
      inputSize: DETECTION_INPUT_SIZE,
      scoreThreshold: DETECTION_SCORE_THRESHOLD,
    }))
    .withFaceLandmarks()
    .withFaceDescriptor();

  if (!detection) {
    return null;
  }

  const box = detection.detection.box;
  const leftEye = detection.landmarks.getLeftEye();
  const rightEye = detection.landmarks.getRightEye();

  return {
    descriptor: Array.from(detection.descriptor),
    centerX: box.x + box.width / 2,
    centerY: box.y + box.height / 2,
    boxWidth: box.width,
    boxHeight: box.height,
    eyeAspectRatio: (calculateEyeAspectRatio(leftEye) + calculateEyeAspectRatio(rightEye)) / 2,
  };
};

const encodeFaceEmbedding = async (video: HTMLVideoElement) => {
  if (video.videoWidth === 0 || video.videoHeight === 0) {
    throw new Error('Camera stream is not ready yet.');
  }

  await loadFaceModels();

  const samples: FaceSample[] = [];

  for (let attempt = 0; attempt < MAX_FACE_SAMPLE_ATTEMPTS; attempt += 1) {
    const sample = await captureFaceSample(video);

    if (sample) {
      samples.push(sample);
    }

    if (samples.length >= REQUIRED_FACE_SAMPLES) {
      break;
    }

    await wait(FACE_SAMPLE_INTERVAL_MS);
  }

  if (samples.length < REQUIRED_FACE_SAMPLES) {
    throw new Error('Keep one face centered in the camera and try again.');
  }

  const livenessScore = calculateLivenessScore(samples);
  if (livenessScore < MIN_LIVENESS_SCORE) {
    throw new Error('Liveness check failed. Blink or slightly move your head and try again.');
  }

  const qualityScore = Math.round(livenessScore * 100);

  return {
    descriptor: encodeDescriptor(averageDescriptors(samples.map((sample) => sample.descriptor))),
    modelName: FACE_MODEL_NAME,
    qualityScore,
    livenessScore,
    sampleCount: samples.length,
  };
};

const describeFaceCapture = (capture: FaceCaptureResult) => {
  return `${capture.modelName}. ${capture.sampleCount} live samples captured. Liveness ${capture.livenessScore}, quality ${capture.qualityScore}/100.`;
};

const formatDate = (value: string) => new Date(value).toLocaleString();

const CitizenPage = () => {
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

  useEffect(() => () => {
    biometricStream?.getTracks().forEach((track) => track.stop());
  }, [biometricStream]);

  const startCamera = async () => {
    setBiometricError('');
    setBiometricStatus('Starting camera...');

    if (!navigator.mediaDevices?.getUserMedia) {
      setBiometricError('This browser does not support camera capture.');
      return;
    }

    let stream: MediaStream | null = null;

    try {
      stream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: 'user',
          width: { ideal: 640 },
          height: { ideal: 480 },
          frameRate: { ideal: 24, max: 30 },
        },
        audio: false,
      });
      setBiometricStream(stream);

      if (videoRef.current) {
        const video = videoRef.current;

        video.muted = true;
        video.playsInline = true;
        video.autoplay = true;
        video.srcObject = stream;

        await new Promise<void>((resolve, reject) => {
          let settled = false;
          let timeoutId = 0;

          const cleanup = () => {
            window.clearTimeout(timeoutId);
            video.removeEventListener('loadedmetadata', complete);
            video.removeEventListener('canplay', complete);
          };

          const complete = () => {
            if (settled || video.readyState < 2 || video.videoWidth === 0) {
              return;
            }

            settled = true;
            cleanup();
            resolve();
          };

          if (video.readyState >= 2 && video.videoWidth > 0) {
            complete();
            return;
          }

          timeoutId = window.setTimeout(() => {
            if (settled) {
              return;
            }

            settled = true;
            cleanup();
            reject(new Error('Camera preview timed out. Close other camera apps and retry.'));
          }, 7000);

          video.addEventListener('loadedmetadata', complete);
          video.addEventListener('canplay', complete);
        });
        await video.play();
      }

      setBiometricStatus('Camera ready. Keep your face centered, then enroll or verify.');
    } catch (error) {
      stream?.getTracks().forEach((track) => track.stop());
      setBiometricStream(null);

      if (videoRef.current) {
        videoRef.current.srcObject = null;
      }

      setBiometricStatus('');
      setBiometricError(error instanceof Error ? error.message : 'Camera permission was denied or the camera is unavailable.');
    }
  };

  const stopCamera = () => {
    biometricStream?.getTracks().forEach((track) => track.stop());
    setBiometricStream(null);

    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
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
      setBiometricStatus('Loading local face model and capturing live face embedding...');
      const capture = await captureFaceDescriptor();
      await actions.enrollBiometric(capture.descriptor);
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
              <Info label="Enrollment Status" value={selectedCitizen?.biometricReference ? 'Face reference captured' : 'No biometric reference captured'} />
              <Info label="Registered Reference" value={getDisplayBiometricReference(selectedCitizen?.biometricReference)} />
            </div>
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
              <Button disabled={!selectedCitizen || state.isPending || !biometricStream} onClick={handleEnrollBiometric}>Enroll face</Button>
              <Button className="primary-button" disabled={!selectedCitizen || state.isPending || !selectedCitizen?.biometricReference || !biometricStream} onClick={handleVerifyBiometric}>Verify face</Button>
              <Button disabled={!biometricStream} onClick={stopCamera}>Stop camera</Button>
            </div>
            {biometricStatus && <p className="biometric-status-text">{biometricStatus}</p>}
            {biometricError && <p className="biometric-error-text">{biometricError}</p>}
            <p className="helper-text">{biometricModelStatus}</p>
            <p className="helper-text">The prototype stores only a compact 128D face embedding reference. It does not store the camera image.</p>
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

