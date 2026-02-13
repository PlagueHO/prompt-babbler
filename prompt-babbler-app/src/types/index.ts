export interface Babble {
  id: string;
  title: string;
  text: string;
  createdAt: string;
  updatedAt: string;
  lastGeneratedPrompt: GeneratedPrompt | null;
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

export interface LlmSettingsView {
  endpoint: string;
  apiKeyHint: string;
  deploymentName: string;
  whisperDeploymentName: string;
  isConfigured: boolean;
}

export interface LlmSettingsSaveRequest {
  endpoint: string;
  apiKey: string;
  deploymentName: string;
  whisperDeploymentName: string;
}

export interface TranscriptionResponse {
  text: string;
  language: string | null;
  duration: number | null;
}

export interface TestConnectionResponse {
  success: boolean;
  message: string;
  latencyMs: number | null;
}
