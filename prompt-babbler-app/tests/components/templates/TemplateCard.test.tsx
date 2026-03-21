import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TemplateCard } from '@/components/templates/TemplateCard';
import type { PromptTemplate } from '@/types';

const builtInTemplate: PromptTemplate = {
  id: 't1',
  name: 'Built-in Template',
  description: 'A built-in template description',
  instructions: 'Do something useful',
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

  it('renders tags when present', () => {
    const taggedTemplate: PromptTemplate = {
      ...builtInTemplate,
      tags: ['coding', 'review'],
    };
    render(<TemplateCard template={taggedTemplate} onClick={vi.fn()} />);
    expect(screen.getByText('coding')).toBeInTheDocument();
    expect(screen.getByText('review')).toBeInTheDocument();
  });
});
