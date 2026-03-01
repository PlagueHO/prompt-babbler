import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ConnectionTest } from '@/components/settings/ConnectionTest';

describe('ConnectionTest', () => {
  it('renders test button', () => {
    render(<ConnectionTest onTest={vi.fn()} />);
    expect(screen.getByRole('button', { name: /test connection/i })).toBeInTheDocument();
  });
});
