import type {
  Babble,
  GeneratedPrompt,
  PagedResponse,
  PromptTemplate,
  StatusResponse,
  UserProfile,
} from '@/types';

// Injected by Vite from Aspire service discovery env vars at build/dev time
declare const __API_BASE_URL__: string;

function getApiBaseUrl(): string {
  return typeof __API_BASE_URL__ !== 'undefined' ? __API_BASE_URL__ : '';
}

function addAuthHeader(headers: HeadersInit = {}, accessToken?: string): HeadersInit {
  if (!accessToken) return headers;
  return { ...headers, Authorization: `Bearer ${accessToken}` };
}

async function fetchJson<T>(
  path: string,
  init?: RequestInit,
  accessToken?: string,
): Promise<T> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}${path}`, {
    ...init,
    headers: addAuthHeader(
      {
        'Content-Type': 'application/json',
        ...init?.headers,
      },
      accessToken,
    ),
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

// Template APIs

export async function getTemplates(forceRefresh = false, accessToken?: string): Promise<PromptTemplate[]> {
  const query = forceRefresh ? '?forceRefresh=true' : '';
  return fetchJson<PromptTemplate[]>(`/api/templates${query}`, undefined, accessToken);
}

export async function getTemplate(id: string, accessToken?: string): Promise<PromptTemplate> {
  return fetchJson<PromptTemplate>(`/api/templates/${encodeURIComponent(id)}`, undefined, accessToken);
}

export async function createTemplate(request: {
  name: string;
  description: string;
  systemPrompt: string;
}, accessToken?: string): Promise<PromptTemplate> {
  return fetchJson<PromptTemplate>('/api/templates', {
    method: 'POST',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function updateTemplate(
  id: string,
  request: { name: string; description: string; systemPrompt: string },
  accessToken?: string,
): Promise<PromptTemplate> {
  return fetchJson<PromptTemplate>(`/api/templates/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function deleteTemplate(id: string, accessToken?: string): Promise<void> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/templates/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
}

export async function generatePrompt(
  babbleText: string,
  templateId: string,
  promptFormat: string = 'text',
  allowEmojis: boolean = false,
  accessToken?: string,
): Promise<ReadableStream<Uint8Array>> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/prompts/generate`, {
    method: 'POST',
    headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
    body: JSON.stringify({ babbleText, templateId, promptFormat, allowEmojis }),
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

// Babble APIs

export async function getBabbles(
  continuationToken?: string | null,
  pageSize = 20,
  accessToken?: string,
): Promise<PagedResponse<Babble>> {
  const params = new URLSearchParams();
  if (continuationToken) params.set('continuationToken', continuationToken);
  params.set('pageSize', String(pageSize));
  const query = params.toString();
  return fetchJson<PagedResponse<Babble>>(`/api/babbles?${query}`, undefined, accessToken);
}

export async function getBabble(id: string, accessToken?: string): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(id)}`, undefined, accessToken);
}

export async function createBabble(
  request: { title: string; text: string },
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>('/api/babbles', {
    method: 'POST',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function updateBabble(
  id: string,
  request: { title: string; text: string },
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function deleteBabble(id: string, accessToken?: string): Promise<void> {
  const base = getApiBaseUrl();
  const res = await fetch(`${base}/api/babbles/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
}

// Generated Prompt APIs

export async function getGeneratedPrompts(
  babbleId: string,
  continuationToken?: string | null,
  pageSize = 20,
  accessToken?: string,
): Promise<PagedResponse<GeneratedPrompt>> {
  const params = new URLSearchParams();
  if (continuationToken) params.set('continuationToken', continuationToken);
  params.set('pageSize', String(pageSize));
  const query = params.toString();
  return fetchJson<PagedResponse<GeneratedPrompt>>(
    `/api/babbles/${encodeURIComponent(babbleId)}/prompts?${query}`,
    undefined,
    accessToken,
  );
}

export async function createGeneratedPrompt(
  babbleId: string,
  request: { templateId: string; templateName: string; promptText: string },
  accessToken?: string,
): Promise<GeneratedPrompt> {
  return fetchJson<GeneratedPrompt>(
    `/api/babbles/${encodeURIComponent(babbleId)}/prompts`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
    accessToken,
  );
}

export async function deleteGeneratedPrompt(
  babbleId: string,
  id: string,
  accessToken?: string,
): Promise<void> {
  const base = getApiBaseUrl();
  const res = await fetch(
    `${base}/api/babbles/${encodeURIComponent(babbleId)}/prompts/${encodeURIComponent(id)}`,
    {
      method: 'DELETE',
      headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
    },
  );
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
}

// User Profile APIs

export async function getUserProfile(accessToken?: string): Promise<UserProfile> {
  return fetchJson<UserProfile>('/api/user', undefined, accessToken);
}

export async function updateUserSettings(
  settings: { theme: string; speechLanguage: string },
  accessToken?: string,
): Promise<UserProfile> {
  return fetchJson<UserProfile>('/api/user/settings', {
    method: 'PUT',
    body: JSON.stringify(settings),
  }, accessToken);
}
