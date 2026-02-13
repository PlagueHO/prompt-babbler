import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TemplateCard } from '@/components/templates/TemplateCard';
import type { PromptTemplate } from '@/types';

const builtInTemplate: PromptTemplate = {
  id: 't1',
  name: 'Built-in Template',
  description: 'A built-in template description',
  systemPrompt: 'Do something useful',
  isBuiltIn: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe('TemplateCard', () => {
  it('renders template name and description', () => {
    render(<TemplateCard template={builtInTemplate} onClick={vi.fn()} />);
    expect(screen.getByText('Built-in Template')).toBeInTheDocument();
    expect(screen.getByText('A built-in template description')).toBeInTheDocument();
  });

  it('shows built-in badge', () => {
    render(<TemplateCard template={builtInTemplate} onClick={vi.fn()} />);
    expect(screen.getByText('Built-in')).toBeInTheDocument();
  });
});
