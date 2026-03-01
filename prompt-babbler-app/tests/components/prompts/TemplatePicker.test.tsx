import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TemplatePicker } from '@/components/prompts/TemplatePicker';
import type { PromptTemplate } from '@/types';

const templates: PromptTemplate[] = [
  {
    id: 't1',
    name: 'Template One',
    description: 'First template',
    systemPrompt: 'prompt1',
    isBuiltIn: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 't2',
    name: 'Template Two',
    description: 'Second template',
    systemPrompt: 'prompt2',
    isBuiltIn: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

describe('TemplatePicker', () => {
  it('renders template options', () => {
    render(
      <TemplatePicker
        templates={templates}
        selectedId="t1"
        onSelect={vi.fn()}
      />
    );
    // The select trigger should show the selected template name
    expect(screen.getByText('Template One')).toBeInTheDocument();
  });
});
