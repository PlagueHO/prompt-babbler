import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { ErrorBanner } from '@/components/ui/error-banner';

expect.extend(toHaveNoViolations);

describe('ErrorBanner', () => {
  it('renders the error message', () => {
    render(<ErrorBanner error="Backend service is not available." />);
    expect(screen.getByText('Backend service is not available.')).toBeInTheDocument();
  });

  it('does not render a retry button when onRetry is not provided', () => {
    render(<ErrorBanner error="Something went wrong." />);
    expect(screen.queryByRole('button', { name: /retry/i })).toBeNull();
  });

  it('renders a retry button when onRetry is provided', () => {
    render(<ErrorBanner error="Something went wrong." onRetry={() => {}} />);
    expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
  });

  it('calls onRetry when the retry button is clicked', async () => {
    const onRetry = vi.fn();
    render(<ErrorBanner error="Something went wrong." onRetry={onRetry} />);
    await userEvent.click(screen.getByRole('button', { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <ErrorBanner error="Backend service is not available." onRetry={() => {}} />,
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
