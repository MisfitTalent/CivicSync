import type * as FaceApi from '@vladmandic/face-api';

export const FACE_MODEL_NAME = 'FaceAPI TinyFaceDetector + 68 landmarks + 128D recognition model';

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

export type FaceCaptureResult = {
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

export const getDisplayBiometricReference = (reference?: string) => {
  if (!reference) {
    return 'Unavailable';
  }

  return reference.split('|')[0] ?? reference;
};

export const describeFaceCapture = (capture: FaceCaptureResult) => {
  return `${capture.modelName}. ${capture.sampleCount} live samples captured. Liveness ${capture.livenessScore}, quality ${capture.qualityScore}/100.`;
};

export const loadFaceModels = async () => {
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

export const wait = (milliseconds: number) => new Promise((resolve) => {
  window.setTimeout(resolve, milliseconds);
});

export const startFaceCamera = async (video: HTMLVideoElement) => {
  if (!navigator.mediaDevices?.getUserMedia) {
    throw new Error('This browser does not support camera capture.');
  }

  const stream = await navigator.mediaDevices.getUserMedia({
    video: {
      facingMode: 'user',
      width: { ideal: 640 },
      height: { ideal: 480 },
      frameRate: { ideal: 24, max: 30 },
    },
    audio: false,
  });

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

  return stream;
};

export const stopFaceCamera = (stream: MediaStream | null, video?: HTMLVideoElement | null) => {
  stream?.getTracks().forEach((track) => track.stop());

  if (video) {
    video.srcObject = null;
  }
};

export const encodeFaceEmbedding = async (video: HTMLVideoElement) => {
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
