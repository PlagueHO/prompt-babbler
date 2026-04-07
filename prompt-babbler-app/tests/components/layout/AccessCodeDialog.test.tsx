import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { AccessCodeDialog } from '@/components/layout/AccessCodeDialog';

expect.extend(toHaveNoViolations);

describe('AccessCodeDialog', () => {
  const defaultProps = {
    open: true,
    onSubmit: vi.fn(),
    isLoading: false,
    error: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and input and button', () => {
    render(<AccessCodeDialog {...defaultProps} />);

    expect(screen.getByText('Access Code Required')).toBeInTheDocument();
    expect(screen.getByLabelText('Access Code')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeInTheDocument();
  });

  it('submits on button click', async () => {
    const onSubmit = vi.fn();
    render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />);

    const input = screen.getByLabelText('Access Code');
    await userEvent.type(input, 'secret123');
    await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

    expect(onSubmit).toHaveBeenCalledWith('secret123');
  });

  it('submits on Enter key', async () => {
    const onSubmit = vi.fn();
    render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />);

    const input = screen.getByLabelText('Access Code');
    await userEvent.type(input, 'secret123{enter}');

    expect(onSubmit).toHaveBeenCalledWith('secret123');
  });

  it('shows loading state', () => {
    render(<AccessCodeDialog {...defaultProps} isLoading={true} />);

    expect(screen.getByRole('button', { name: 'Verifying...' })).toBeDisabled();
  });

  it('displays error message', () => {
    render(<AccessCodeDialog {...defaultProps} error="Invalid access code" />);

    expect(screen.getByRole('alert')).toHaveTextContent('Invalid access code');
  });

  it('disables submit when input is empty', () => {
    render(<AccessCodeDialog {...defaultProps} />);

    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
  });

  it('passes accessibility checks', async () => {
    const { container } = render(<AccessCodeDialog {...defaultProps} />);

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
