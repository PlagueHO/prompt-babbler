import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { TagInput } from '@/components/ui/tag-input';

expect.extend(toHaveNoViolations);

describe('TagInput', () => {
  it('renders initial tags as badges', () => {
    render(<TagInput value={['react', 'typescript']} onChange={vi.fn()} />);
    expect(screen.getByText('react')).toBeInTheDocument();
    expect(screen.getByText('typescript')).toBeInTheDocument();
  });

  it('adds a tag on Enter keypress', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, 'newTag{Enter}');

    expect(onChange).toHaveBeenCalledWith(['newTag']);
  });

  it('trims whitespace from tags', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, '  spaced  {Enter}');

    expect(onChange).toHaveBeenCalledWith(['spaced']);
  });

  it('removes a tag when clicking the remove button', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['react', 'vue']} onChange={onChange} />);

    const removeButton = screen.getByLabelText('Remove tag: react');
    await userEvent.click(removeButton);

    expect(onChange).toHaveBeenCalledWith(['vue']);
  });

  it('removes the last tag on Backspace when input is empty', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['first', 'last']} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.click(input);
    await userEvent.keyboard('{Backspace}');

    expect(onChange).toHaveBeenCalledWith(['first']);
  });

  it('rejects duplicate tags (case-insensitive)', async () => {
    const onChange = vi.fn();
    render(<TagInput value={['React']} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, 'react{Enter}');

    expect(onChange).not.toHaveBeenCalled();
  });

  it('enforces maxTags limit', () => {
    const tags = Array.from({ length: 5 }, (_, i) => `tag${i}`);
    render(<TagInput value={tags} onChange={vi.fn()} maxTags={5} />);

    expect(screen.queryByLabelText('Add tag')).not.toBeInTheDocument();
    expect(screen.getByText('Maximum 5 tags')).toBeInTheDocument();
  });

  it('enforces maxTagLength by truncating', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} maxTagLength={5} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, 'toolongtag{Enter}');

    expect(onChange).toHaveBeenCalledWith(['toolo']);
  });

  it('handles paste with comma-separated values', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.click(input);
    await userEvent.paste('one, two, three');

    expect(onChange).toHaveBeenCalledWith(['one', 'two', 'three']);
  });

  it('adds tag on blur when input has value', async () => {
    const onChange = vi.fn();
    render(<TagInput value={[]} onChange={onChange} />);

    const input = screen.getByLabelText('Add tag');
    await userEvent.type(input, 'blurTag');
    await userEvent.tab();

    expect(onChange).toHaveBeenCalledWith(['blurTag']);
  });

  it('renders disabled state without input or remove buttons', () => {
    render(
      <TagInput value={['react', 'vue']} onChange={vi.fn()} disabled />,
    );

    expect(screen.getByText('react')).toBeInTheDocument();
    expect(screen.getByText('vue')).toBeInTheDocument();
    expect(screen.queryByLabelText('Add tag')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Remove tag: react')).not.toBeInTheDocument();
  });

  it('shows placeholder only when no tags exist', () => {
    const { rerender } = render(
      <TagInput value={[]} onChange={vi.fn()} placeholder="Add tags here" />,
    );

    expect(screen.getByPlaceholderText('Add tags here')).toBeInTheDocument();

    rerender(
      <TagInput value={['existing']} onChange={vi.fn()} placeholder="Add tags here" />,
    );

    expect(screen.queryByPlaceholderText('Add tags here')).not.toBeInTheDocument();
  });

  it('passes accessibility checks', async () => {
    const { container } = render(
      <TagInput value={['react', 'vue']} onChange={vi.fn()} />,
    );

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
