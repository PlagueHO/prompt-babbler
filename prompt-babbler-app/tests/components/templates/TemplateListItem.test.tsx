import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TemplateListItem } from '@/components/templates/TemplateListItem';
import type { PromptTemplate } from '@/types';

const template: PromptTemplate = {
  id: 't1',
  name: 'Writer Template',
  description: 'Short description for list view',
  instructions: 'Long full content that should not appear in the list row',
  tags: ['creative'],
  isBuiltIn: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe('TemplateListItem', () => {
  it('renders title, description, and tags without full instructions', () => {
    render(
      <table>
        <tbody>
          <TemplateListItem template={template} onSelect={vi.fn()} />
        </tbody>
      </table>,
    );

    expect(screen.getByText('Writer Template')).toBeInTheDocument();
    expect(screen.getByText('Short description for list view')).toBeInTheDocument();
    expect(screen.getByText('creative')).toBeInTheDocument();
    expect(screen.queryByText('Long full content that should not appear in the list row')).not.toBeInTheDocument();
  });
});
