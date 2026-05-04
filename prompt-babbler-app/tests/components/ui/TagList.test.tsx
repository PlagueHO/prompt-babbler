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

  it('renders tags with deterministic color classes', () => {
    render(<TagList tags={['bug', 'feature']} />);
    const badges = screen.getAllByText(/bug|feature/);
    badges.forEach((badge) => {
      expect(badge.className).toMatch(/bg-\w+-\d+/);
      expect(badge.className).toMatch(/text-\w+-\d+/);
    });
  });

  it('renders the same tag with the same color class', () => {
    const { rerender } = render(<TagList tags={['bug']} />);
    const firstColor = screen.getByText('bug').className;
    rerender(<TagList tags={['other', 'bug']} />);
    const secondColor = screen.getByText('bug').className;
    expect(firstColor).toBe(secondColor);
  });
});
