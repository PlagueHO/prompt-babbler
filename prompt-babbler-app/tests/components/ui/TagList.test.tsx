import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TagList } from '@/components/ui/tag-list';

describe('TagList', () => {
  it('renders nothing when tags is undefined', () => {
    const { container } = render(<TagList tags={undefined} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders nothing when tags array is empty', () => {
    const { container } = render(<TagList tags={[]} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders all tags as badges', () => {
    render(<TagList tags={['alpha', 'beta', 'gamma']} />);
    expect(screen.getByText('alpha')).toBeInTheDocument();
    expect(screen.getByText('beta')).toBeInTheDocument();
    expect(screen.getByText('gamma')).toBeInTheDocument();
  });

  it('applies custom className', () => {
    const { container } = render(<TagList tags={['tag1']} className="mt-4" />);
    expect(container.firstChild).toHaveClass('mt-4');
  });
});
