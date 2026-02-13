import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { RecordButton } from '@/components/recording/RecordButton';

describe('RecordButton', () => {
  it('renders the record button', () => {
    render(
      <RecordButton
        isRecording={false}
        onStart={vi.fn()}
        onStop={vi.fn()}
      />
    );
    const button = screen.getByRole('button');
    expect(button).toBeInTheDocument();
  });

  it('renders without crashing', () => {
    const { container } = render(
      <RecordButton
        isRecording={true}
        onStart={vi.fn()}
        onStop={vi.fn()}
      />
    );
    expect(container).toBeTruthy();
  });
});
