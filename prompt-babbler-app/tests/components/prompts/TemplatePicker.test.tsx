import { beforeAll, describe, it, expect, vi } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { TemplatePicker } from '@/components/prompts/TemplatePicker';
import type { PromptTemplate } from '@/types';

const templates: PromptTemplate[] = [
  {
    id: 't1',
    name: 'Template One',
    description: 'First template',
    instructions: 'prompt1',
    tags: ['summary', 'short'],
    isBuiltIn: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
  {
    id: 't2',
    name: 'Template Two',
    description: 'Second template',
    instructions: 'prompt2',
    tags: ['creative'],
    isBuiltIn: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  },
];

beforeAll(() => {
  class MockResizeObserver {
    observe(): void {}
    unobserve(): void {}
    disconnect(): void {}
  }

  global.ResizeObserver = MockResizeObserver;
  Element.prototype.scrollIntoView = vi.fn();
});

describe('TemplatePicker', () => {
  it('shows selected template in trigger button', () => {
    render(
      <TemplatePicker
        templates={templates}
        selectedId="t1"
        onSelect={vi.fn()}
      />
    );
    expect(screen.getByText('Template One')).toBeInTheDocument();
  });

  it('renders template metadata in template browser', () => {
    render(
      <TemplatePicker
        templates={templates}
        selectedId="t1"
        onSelect={vi.fn()}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: /template one/i }));
    const dialog = screen.getByRole('dialog');

    expect(within(dialog).getByText('First template')).toBeInTheDocument();
    expect(within(dialog).getByText('summary')).toBeInTheDocument();
    expect(within(dialog).getByText('Template Two')).toBeInTheDocument();
    expect(within(dialog).getByRole('button', { name: 'Built-in' })).toBeInTheDocument();
    expect(within(dialog).getAllByText('Built-in', { selector: 'span' })).toHaveLength(1);
  });

  it('filters templates by search and type filter', () => {
    render(
      <TemplatePicker
        templates={templates}
        selectedId="t1"
        onSelect={vi.fn()}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: /template one/i }));
    const dialog = screen.getByRole('dialog');
    fireEvent.input(
      within(dialog).getByPlaceholderText('Search templates by name, description, or tag...'),
      { target: { value: 'creative' } }
    );

    expect(within(dialog).getByText('Template Two')).toBeInTheDocument();
    expect(within(dialog).queryByText('First template')).not.toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Built-in' }));

    expect(within(dialog).queryByText('Template Two')).not.toBeInTheDocument();
    expect(within(dialog).getByText('No templates match your filters.')).toBeInTheDocument();
  });

  it('selects a template from the browser', () => {
    const onSelect = vi.fn();
    render(
      <TemplatePicker
        templates={templates}
        selectedId="t1"
        onSelect={onSelect}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: /template one/i }));
    fireEvent.click(screen.getByText('Template Two'));

    expect(onSelect).toHaveBeenCalledWith('t2');
  });
});
