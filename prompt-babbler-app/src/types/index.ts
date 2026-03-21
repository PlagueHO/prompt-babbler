export interface Babble {
  id: string;
  title: string;
  text: string;
  tags?: string[];
  createdAt: string;
  updatedAt: string;
}

export interface PromptExample {
  input: string;
  output: string;
}

export interface PromptTemplate {
  id: string;
  name: string;
  description: string;
  instructions: string;
  outputDescription?: string;
  outputTemplate?: string;
  examples?: PromptExample[];
  guardrails?: string[];
  defaultOutputFormat?: PromptFormat;
  defaultAllowEmojis?: boolean;
  tags?: string[];
  additionalProperties?: Record<string, unknown>;
  isBuiltIn: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface GeneratedPrompt {
  id: string;
  babbleId: string;
  templateId: string;
  templateName: string;
  promptText: string;
  generatedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  continuationToken: string | null;
}

export interface StatusResponse {
  status: string;
}

export interface UserSettings {
  theme: ThemeMode;
  speechLanguage: string;
}

export interface UserProfile {
  id: string;
  displayName: string | null;
  email: string | null;
  settings: UserSettings;
  createdAt: string;
  updatedAt: string;
}

export type PromptFormat = 'text' | 'markdown';

export type ThemeMode = 'light' | 'dark' | 'system';

export interface PromptOptions {
  promptFormat: PromptFormat;
  allowEmojis: boolean;
}
