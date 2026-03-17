export interface Babble {
  id: string;
  title: string;
  text: string;
  createdAt: string;
  updatedAt: string;
}

export interface PromptTemplate {
  id: string;
  name: string;
  description: string;
  systemPrompt: string;
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

export type PromptFormat = 'text' | 'markdown';

export type ThemeMode = 'light' | 'dark' | 'system';

export interface PromptOptions {
  promptFormat: PromptFormat;
  allowEmojis: boolean;
}
