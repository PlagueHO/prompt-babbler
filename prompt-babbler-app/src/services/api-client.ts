import type {
  StatusResponse,
} from '@/types';

// Injected by Vite from Aspire service discovery env vars at build/dev time
declare const __API_BASE_URL__: string;

function getApiBaseUrl(): string {
  return typeof __API_BASE_URL__ !== 'undefined' ? __API_BASE_URL__ : '';
}

async function fetchJson<T>(
  path: string,
  init?: RequestInit
): Promise<T> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
  return res.json() as Promise<T>;
}

export async function getStatus(): Promise<StatusResponse> {
  return fetchJson<StatusResponse>('/api/status');
}

export async function generatePrompt(
  babbleText: string,
  systemPrompt: string
): Promise<ReadableStream<Uint8Array>> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/prompts/generate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ babbleText, systemPrompt }),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Generation error ${res.status}: ${text}`);
  }
  if (!res.body) {
    throw new Error('No response body for streaming');
  }
  return res.body;
}
