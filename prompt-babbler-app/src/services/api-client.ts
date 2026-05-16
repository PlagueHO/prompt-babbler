import type {
  AccessControlStatus,
  BabbleSearchResponse,
  Babble,
  GeneratedPrompt,
  ImportExportJob,
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

// Module-scoped access code state
let currentAccessCode: string | null = null;

export function setAccessCode(code: string | null): void {
  currentAccessCode = code;
}

export function getAccessCode(): string | null {
  return currentAccessCode;
}

function buildHeaders(contentType?: string, accessToken?: string): Record<string, string> {
  const headers: Record<string, string> = {};
  if (contentType) {
    headers['Content-Type'] = contentType;
  }
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (currentAccessCode) {
    headers['X-Access-Code'] = currentAccessCode;
  }
  return headers;
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
      headers: {
        ...buildHeaders('application/json', accessToken),
        ...init?.headers,
      },
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
  return fetchJson<StatusResponse>('/health');
}

export async function getAccessStatus(): Promise<AccessControlStatus> {
  return fetchJson<AccessControlStatus>('/api/config/access-status');
}

// Template APIs

export interface ListTemplatesOptions {
  continuationToken?: string | null;
  pageSize?: number;
  search?: string;
  tag?: string;
  sortBy?: 'name' | 'updatedAt';
  sortDirection?: 'desc' | 'asc';
  forceRefresh?: boolean;
}

export async function listTemplates(
  options: ListTemplatesOptions = {},
  accessToken?: string,
): Promise<PagedResponse<PromptTemplate>> {
  const params = new URLSearchParams();
  if (options.continuationToken) params.set('continuationToken', options.continuationToken);
  params.set('pageSize', String(options.pageSize ?? 20));
  if (options.search) params.set('search', options.search);
  if (options.tag) params.set('tag', options.tag);
  if (options.sortBy) params.set('sortBy', options.sortBy);
  if (options.sortDirection) params.set('sortDirection', options.sortDirection);
  if (options.forceRefresh) params.set('forceRefresh', 'true');
  const query = params.toString();
  return fetchJson<PagedResponse<PromptTemplate>>(`/api/templates?${query}`, undefined, accessToken);
}

export async function getTemplates(forceRefresh = false, accessToken?: string): Promise<PromptTemplate[]> {
  const templates: PromptTemplate[] = [];
  let continuationToken: string | null = null;
  let isFirstPage = true;

  do {
    const page = await listTemplates(
      {
        continuationToken,
        pageSize: 100,
        forceRefresh: isFirstPage && forceRefresh,
      },
      accessToken,
    );
    templates.push(...page.items);
    continuationToken = page.continuationToken;
    isFirstPage = false;
  } while (continuationToken);

  return templates;
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
      headers: buildHeaders('application/json', accessToken),
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
      headers: buildHeaders('application/json', accessToken),
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
      headers: buildHeaders('application/json', accessToken),
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
        headers: buildHeaders('application/json', accessToken),
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

// Semantic Search APIs

export async function searchBabbles(
  query: string,
  topK: number = 10,
  signal?: AbortSignal,
  accessToken?: string,
): Promise<BabbleSearchResponse> {
  const params = new URLSearchParams();
  params.set('query', query);
  params.set('topK', String(topK));
  return fetchJson<BabbleSearchResponse>(
    `/api/babbles/search?${params.toString()}`,
    { signal },
    accessToken,
  );
}

export async function uploadAudioFile(
  file: File,
  title?: string,
  accessToken?: string,
): Promise<Babble> {
  const base = getApiBaseUrl();
  const formData = new FormData();
  formData.append('file', file);
  if (title) {
    formData.append('title', title);
  }

  const headers: Record<string, string> = {};
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (currentAccessCode) {
    headers['X-Access-Code'] = currentAccessCode;
  }
  // Do NOT set Content-Type — browser auto-sets multipart boundary

  let res: Response;
  try {
    res = await fetch(`${base}/api/babbles/upload`, {
      method: 'POST',
      headers,
      body: formData,
    });
  } catch {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (isHtmlResponse(res)) {
    throw new Error(BACKEND_UNAVAILABLE_MSG);
  }
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText);
    throw new Error(`Upload failed (${res.status}): ${text}`);
  }
  return res.json() as Promise<Babble>;
}

// Import/Export APIs

interface StartJobResponse {
  jobId: string;
}

export interface StartExportRequest {
  includeBabbles: boolean;
  includeGeneratedPrompts: boolean;
  includeUserTemplates: boolean;
  includeSemanticVectors: boolean;
}

export async function startExport(
  request: StartExportRequest,
  accessToken?: string,
): Promise<string> {
  const response = await fetchJson<StartJobResponse>('/api/exports', {
    method: 'POST',
    body: JSON.stringify(request),
  }, accessToken);

  return response.jobId;
}

export async function getExportJob(jobId: string, accessToken?: string): Promise<ImportExportJob> {
  return fetchJson<ImportExportJob>(`/api/exports/${encodeURIComponent(jobId)}`, undefined, accessToken);
}

export async function downloadExport(jobId: string, accessToken?: string): Promise<Blob> {
  const base = getApiBaseUrl();
  let res: Response;

  try {
    res = await fetch(`${base}/api/exports/${encodeURIComponent(jobId)}/download`, {
      method: 'GET',
      headers: buildHeaders(undefined, accessToken),
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

  return res.blob();
}

export async function startImport(
  file: File,
  overwrite: boolean,
  accessToken?: string,
): Promise<string> {
  const base = getApiBaseUrl();
  const formData = new FormData();
  formData.append('file', file);

  const headers: Record<string, string> = {};
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (currentAccessCode) {
    headers['X-Access-Code'] = currentAccessCode;
  }

  let res: Response;
  try {
    res = await fetch(`${base}/api/imports?overwrite=${overwrite}`, {
      method: 'POST',
      headers,
      body: formData,
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

  const response = await res.json() as StartJobResponse;
  return response.jobId;
}

export async function getImportJob(jobId: string, accessToken?: string): Promise<ImportExportJob> {
  return fetchJson<ImportExportJob>(`/api/imports/${encodeURIComponent(jobId)}`, undefined, accessToken);
}
