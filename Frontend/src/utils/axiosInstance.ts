declare global {
  interface Window {
    __CIVICSYNC_CONFIG__?: {
      apiKey?: string;
      homeAffairsApiUrl?: string;
      sarsApiUrl?: string;
      municipalityApiUrl?: string;
    };
  }
}

const getApiKey = () => {
  const runtimeApiKey = typeof window !== 'undefined'
    ? window.__CIVICSYNC_CONFIG__?.apiKey
    : undefined;

  return runtimeApiKey?.trim()
    || process.env.NEXT_PUBLIC_CIVICSYNC_API_KEY
    || 'development-civicsync-api-key';
};

interface CivicSyncHttpResponse<T> {
  data: T;
}

interface ErrorResponse {
  detail?: string;
  title?: string;
}

const request = async <T>(baseUrl: string, path: string, method: 'GET' | 'POST', body?: unknown): Promise<CivicSyncHttpResponse<T>> => {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'X-CivicSync-Api-Key': getApiKey(),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    const contentType = response.headers.get('content-type') || '';

    if (contentType.includes('application/json')) {
      const data = await response.json() as ErrorResponse;
      message = data.detail || data.title || message;
    } else {
      const text = await response.text();
      message = text || message;
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return { data: undefined as T };
  }

  return { data: await response.json() as T };
};

const getBlob = async (baseUrl: string, path: string): Promise<Blob> => {
  const response = await fetch(`${baseUrl}${path}`, {
    method: 'GET',
    headers: {
      'X-CivicSync-Api-Key': getApiKey(),
    },
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }

  return response.blob();
};

export const createCivicSyncHttpClient = (baseUrl: string) => ({
  get: <T>(path: string) => request<T>(baseUrl, path, 'GET'),
  getBlob: (path: string) => getBlob(baseUrl, path),
  post: <T>(path: string, body?: unknown) => request<T>(baseUrl, path, 'POST', body),
});

export const getErrorMessage = (error: unknown) => {
  return error instanceof Error ? error.message : 'Something went wrong.';
};
