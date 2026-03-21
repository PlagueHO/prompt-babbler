import type {
  Babble,
  GeneratedPrompt,
  PagedResponse,
  PromptFormat,
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

function isHtmlResponse(res: Response): boolean {
  const ct = res.headers.get('content-type') ?? '';
  return ct.includes('text/html');
}

const BACKEND_UNAVAILABLE_MSG = 'Backend service is not available. Please start the backend and try again.';

async function fetchJson<T>(
  path: string,
  init?: RequestInit,
  accessToken?: string,
): Promise<T> {
  const base = getApiBaseUrl();
  let res: Response;
  try {
    res = await fetch(`${base}${path}`, {
      ...init,
      headers: addAuthHeader(
        {
          'Content-Type': 'application/json',
          ...init?.headers,
        },
        accessToken,
      ),
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
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

export interface TemplateRequest {
  name: string;
  description: string;
  instructions: string;
  outputDescription?: string;
  outputTemplate?: string;
  examples?: { input: string; output: string }[];
  guardrails?: string[];
  defaultOutputFormat?: PromptFormat;
  defaultAllowEmojis?: boolean;
  tags?: string[];
  additionalProperties?: Record<string, unknown>;
}

export async function createTemplate(
  request: TemplateRequest,
  accessToken?: string,
): Promise<PromptTemplate> {
  return fetchJson<PromptTemplate>('/api/templates', {
    method: 'POST',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function updateTemplate(
  id: string,
  request: TemplateRequest,
  accessToken?: string,
): Promise<PromptTemplate> {
  return fetchJson<PromptTemplate>(`/api/templates/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function deleteTemplate(id: string, accessToken?: string): Promise<void> {
  const base = getApiBaseUrl();
  let res: Response;
  try {
    res = await fetch(`${base}/api/templates/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`API error ${res.status}: ${text}`);
  }
}

export async function generatePrompt(
  babbleId: string,
  templateId: string,
  promptFormat: string = 'text',
  allowEmojis: boolean = false,
  accessToken?: string,
): Promise<ReadableStream<Uint8Array>> {
  const base = getApiBaseUrl();
  let res: Response;
  try {
    res = await fetch(`${base}/api/babbles/${encodeURIComponent(babbleId)}/generate`, {
      method: 'POST',
      headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
      body: JSON.stringify({ templateId, promptFormat, allowEmojis }),
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Generation error ${res.status}: ${text}`);
  }
  if (!res.body) {
    throw new Error('No response body for streaming');
  }
  return res.body;
}

export async function generateTitle(
  babbleId: string,
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(babbleId)}/generate-title`, {
    method: 'POST',
  }, accessToken);
}

// Babble APIs

export interface GetBabblesOptions {
  continuationToken?: string | null;
  pageSize?: number;
  search?: string;
  sortBy?: 'createdAt' | 'title';
  sortDirection?: 'desc' | 'asc';
  isPinned?: boolean;
}

export async function getBabbles(
  options: GetBabblesOptions = {},
  accessToken?: string,
): Promise<PagedResponse<Babble>> {
  const params = new URLSearchParams();
  if (options.continuationToken) params.set('continuationToken', options.continuationToken);
  params.set('pageSize', String(options.pageSize ?? 20));
  if (options.search) params.set('search', options.search);
  if (options.sortBy) params.set('sortBy', options.sortBy);
  if (options.sortDirection) params.set('sortDirection', options.sortDirection);
  if (options.isPinned !== undefined) params.set('isPinned', String(options.isPinned));
  const query = params.toString();
  return fetchJson<PagedResponse<Babble>>(`/api/babbles?${query}`, undefined, accessToken);
}

export async function getBabble(id: string, accessToken?: string): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(id)}`, undefined, accessToken);
}

export async function createBabble(
  request: { title: string; text: string; tags?: string[] },
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>('/api/babbles', {
    method: 'POST',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function updateBabble(
  id: string,
  request: { title: string; text: string; tags?: string[]; isPinned?: boolean },
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  }, accessToken);
}

export async function pinBabble(
  id: string,
  isPinned: boolean,
  accessToken?: string,
): Promise<Babble> {
  return fetchJson<Babble>(`/api/babbles/${encodeURIComponent(id)}/pin`, {
    method: 'PATCH',
    body: JSON.stringify({ isPinned }),
  }, accessToken);
}

export async function deleteBabble(id: string, accessToken?: string): Promise<void> {
  const base = getApiBaseUrl();
  let res: Response;
  try {
    res = await fetch(`${base}/api/babbles/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
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
  let res: Response;
  try {
    res = await fetch(
      `${base}/api/babbles/${encodeURIComponent(babbleId)}/prompts/${encodeURIComponent(id)}`,
      {
        method: 'DELETE',
        headers: addAuthHeader({ 'Content-Type': 'application/json' }, accessToken),
      },
    );
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
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
