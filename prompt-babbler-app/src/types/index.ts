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

export interface StatusResponse {
  status: string;
}
