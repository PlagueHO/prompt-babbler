import { describe, it, expect, vi, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TemplateEditor } from '@/components/templates/TemplateEditor';
import type { PromptTemplate } from '@/types';

beforeAll(() => {
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

const baseTemplate: PromptTemplate = {
  id: 't1',
  name: 'Test Template',
  description: 'A test template',
  instructions: 'Test instructions',
  isBuiltIn: false,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

describe('TemplateEditor — Tags', () => {
  it('renders existing tags as badge chips', () => {
    const template: PromptTemplate = {
      ...baseTemplate,
      tags: ['react', 'typescript'],
    };
    render(
      <TemplateEditor
        template={template}
        onSave={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    expect(screen.getByText('react')).toBeInTheDocument();
    expect(screen.getByText('typescript')).toBeInTheDocument();
  });

  it('allows adding a new tag via Enter key', async () => {
    render(
      <TemplateEditor
        template={baseTemplate}
        onSave={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, 'newTag{Enter}');

    expect(screen.getByText('newTag')).toBeInTheDocument();
  });

  it('includes tags as string array in onSave', async () => {
    const onSave = vi.fn();
    const template: PromptTemplate = {
      ...baseTemplate,
      tags: ['existing'],
    };
    render(
      <TemplateEditor
        template={template}
        onSave={onSave}
        onCancel={vi.fn()}
      />,
    );

    const saveButton = screen.getByRole('button', { name: /save/i });
    await userEvent.click(saveButton);

    await waitFor(() => {
      expect(onSave).toHaveBeenCalledTimes(1);
    });

    const savedRequest = onSave.mock.calls[0][0];
    expect(savedRequest.tags).toEqual(['existing']);
  });

  it('shows tags as read-only for built-in templates', () => {
    const template: PromptTemplate = {
      ...baseTemplate,
      isBuiltIn: true,
      tags: ['coding', 'assistant'],
    };
    render(
      <TemplateEditor
        template={template}
        onSave={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    expect(screen.getByText('coding')).toBeInTheDocument();
    expect(screen.getByText('assistant')).toBeInTheDocument();
    expect(screen.queryByLabelText('Add tag')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Remove tag: coding')).not.toBeInTheDocument();
  });
});
