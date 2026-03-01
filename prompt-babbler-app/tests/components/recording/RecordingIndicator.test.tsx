import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { RecordingIndicator } from '@/components/recording/RecordingIndicator';

describe('RecordingIndicator', () => {
  it('renders start control when not recording', () => {
    render(
      <RecordingIndicator
        isRecording={false}
        duration={0}
        onStart={vi.fn()}
        onStop={vi.fn()}
      />
    );
    expect(screen.getByRole('button', { name: /start recording/i })).toBeInTheDocument();
  });

  it('renders stop control when recording', () => {
    render(
      <RecordingIndicator
        isRecording={true}
        duration={5}
        onStart={vi.fn()}
        onStop={vi.fn()}
      />
    );
    expect(screen.getByRole('button', { name: /stop/i })).toBeInTheDocument();
    expect(screen.getByText('Recording')).toBeInTheDocument();
    expect(screen.getByText('00:05')).toBeInTheDocument();
  });
});
