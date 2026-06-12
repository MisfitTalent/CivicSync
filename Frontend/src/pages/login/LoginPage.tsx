import { Button, Card, Input } from 'antd';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthActions, useAuthState } from '../../providers/authProvider';
import { useCivicSyncActions } from '../../providers/civicSyncProvider';
import { nodes } from '../../providers/civicSyncProvider/context';
import { describeFaceCapture, encodeFaceEmbedding, FACE_MODEL_NAME, startFaceCamera, stopFaceCamera } from '../../utils/faceRecognition';

type LoginAuthMethod = 'password' | 'passkey' | 'face';
type LoginMode = 'signIn' | 'register';

const LoginPage = () => {
  const navigate = useNavigate();
  const { registerAccount, registerPasskey, signIn, signInWithFace, signInWithPasskey } = useAuthActions();
  const authState = useAuthState();
  const { setActiveNode } = useCivicSyncActions();
  const [loginMode, setLoginMode] = useState<LoginMode>('signIn');
  const [displayName, setDisplayName] = useState('');
  const [emailAddress, setEmailAddress] = useState('');
  const [password, setPassword] = useState('');
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const [faceStream, setFaceStream] = useState<MediaStream | null>(null);
  const [isFaceCameraStarting, setIsFaceCameraStarting] = useState(false);
  const [lastAuthMethod, setLastAuthMethod] = useState<LoginAuthMethod>('password');
  const [faceStatus, setFaceStatus] = useState(`${FACE_MODEL_NAME} ready.`);
  const [faceError, setFaceError] = useState('');

  useEffect(() => {
    return () => {
      faceStream?.getTracks().forEach((track) => track.stop());
    };
  }, [faceStream]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLastAuthMethod('password');
    const profile = loginMode === 'register'
      ? registerAccount(displayName, emailAddress, password)
      : signIn(emailAddress, password);

    if (!profile) {
      return;
    }

    if (profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    navigate(profile.workspacePath, { replace: true });
  };

  const completePasskeyFlow = (profile: ReturnType<typeof signIn>) => {
    if (!profile) {
      return;
    }

    if (profile.departmentCode) {
      const departmentNode = nodes.find((node) => node.departmentCode === profile.departmentCode);
      if (departmentNode) {
        setActiveNode(departmentNode);
      }
    }

    navigate(profile.workspacePath, { replace: true });
  };

  const handleRegisterPasskey = async () => {
    setLastAuthMethod('passkey');
    const profile = await registerPasskey(emailAddress, password);
    completePasskeyFlow(profile);
  };

  const handlePasskeySignIn = async () => {
    setLastAuthMethod('passkey');
    const profile = await signInWithPasskey(emailAddress);
    completePasskeyFlow(profile);
  };

  const handleStartFaceCamera = async () => {
    setFaceError('');
    setIsFaceCameraStarting(true);
    setFaceStatus('Starting camera...');

    try {
      if (!videoRef.current) {
        throw new Error('Camera preview is not ready.');
      }

      const stream = await startFaceCamera(videoRef.current);
      setFaceStream(stream);
      setFaceStatus('Camera ready. Keep your enrolled face centered, blink or slightly move, then choose Face login.');
    } catch (error) {
      setFaceStream(null);
      setFaceStatus('');
      setFaceError(error instanceof Error ? error.message : 'Camera permission was denied or the camera is unavailable.');
    } finally {
      setIsFaceCameraStarting(false);
    }
  };

  const handleStopFaceCamera = () => {
    stopFaceCamera(faceStream, videoRef.current);
    setFaceStream(null);
    setFaceStatus(`${FACE_MODEL_NAME} ready.`);
  };

  const handleFaceSignIn = async () => {
    setLastAuthMethod('face');
    setFaceError('');

    try {
      if (!videoRef.current || !faceStream) {
        throw new Error('Start the camera before using face login.');
      }

      setFaceStatus('Capturing live face and verifying with CivicSync...');
      const capture = await encodeFaceEmbedding(videoRef.current);
      setFaceStatus(describeFaceCapture(capture));
      const profile = await signInWithFace(emailAddress, capture.descriptor);
      completePasskeyFlow(profile);
    } catch (error) {
      setFaceStatus('');
      setFaceError(error instanceof Error ? error.message : 'Face login failed.');
    }
  };

  return (
    <main className="login-shell">
      <section className="login-panel">
        <div className="login-copy">
          <img className="login-logo" src="/civicsync-logo.svg" alt="CivicSync Ledger logo" />
          <p className="eyebrow">CivicSync Ledger</p>
          <h1>Sign in</h1>
          <p>Access your CivicSync workspace to manage records, approvals, ledger activity, and department sync.</p>
        </div>

        <Card className="login-card">
          <h2>Account Login</h2>
          <form className="form-stack" onSubmit={handleSubmit}>
            {loginMode === 'register' && (
              <label>
                <span>Full name</span>
                <Input value={displayName} onChange={(event) => setDisplayName(event.target.value)} required />
              </label>
            )}
            <label>
              <span>Email address</span>
              <Input type="email" value={emailAddress} onChange={(event) => setEmailAddress(event.target.value)} required />
            </label>
            <label>
              <span>Password</span>
              <Input.Password value={password} onChange={(event) => setPassword(event.target.value)} required />
            </label>
            {authState.isError && lastAuthMethod !== 'face' && <p className="form-error" role="alert">{authState.errorMessage}</p>}
            <Button className="primary-button" htmlType="submit" disabled={authState.isPending}>
              {loginMode === 'register' ? 'Create account' : 'Sign in'}
            </Button>
            <Button
              type="default"
              htmlType="button"
              disabled={authState.isPending}
              onClick={() => {
                setLastAuthMethod('password');
                setLoginMode((currentMode) => currentMode === 'register' ? 'signIn' : 'register');
              }}
            >
              {loginMode === 'register' ? 'Back to sign in' : 'Register new account'}
            </Button>
            {loginMode === 'signIn' && (
              <>
                <div className="passkey-actions">
                  <Button type="default" htmlType="button" disabled={authState.isPending || !emailAddress.trim()} onClick={handlePasskeySignIn}>
                    Use device passkey
                  </Button>
                  <Button type="default" htmlType="button" disabled={authState.isPending || !emailAddress.trim() || !password} onClick={handleRegisterPasskey}>
                    Register passkey
                  </Button>
                </div>
                <p className="helper-text">Passkeys use your device authenticator, such as Windows Hello, Face ID, or fingerprint, without sending biometric data to CivicSync.</p>
              </>
            )}

            {loginMode === 'signIn' && (
              <div className="face-login-panel">
                <div className="biometric-camera-panel login-face-camera">
                  <video
                    ref={videoRef}
                    className={`biometric-video ${faceStream ? '' : 'biometric-video-idle'}`}
                    aria-label="Face login camera preview"
                    autoPlay
                    muted
                    playsInline
                  />
                  {!faceStream && (
                    <div className="biometric-placeholder">
                      {isFaceCameraStarting ? 'Waiting for browser camera permission' : 'Start camera to use face login'}
                    </div>
                  )}
                </div>
                <div className="biometric-action-row">
                  <Button type="default" htmlType="button" disabled={authState.isPending || isFaceCameraStarting || Boolean(faceStream)} onClick={handleStartFaceCamera}>
                    Start camera
                  </Button>
                  <Button className="primary-button" htmlType="button" disabled={authState.isPending || isFaceCameraStarting || !emailAddress.trim() || !faceStream} onClick={handleFaceSignIn}>
                    Face login
                  </Button>
                  <Button type="default" htmlType="button" disabled={isFaceCameraStarting || !faceStream} onClick={handleStopFaceCamera}>
                    Stop camera
                  </Button>
                </div>
                {faceStatus && <p className="biometric-status-text">{faceStatus}</p>}
                {faceError && <p className="biometric-error-text" role="alert">{faceError}</p>}
                {authState.isError && lastAuthMethod === 'face' && <p className="biometric-error-text" role="alert">{authState.errorMessage}</p>}
              </div>
            )}
          </form>
        </Card>
      </section>
    </main>
  );
};

export default LoginPage;
