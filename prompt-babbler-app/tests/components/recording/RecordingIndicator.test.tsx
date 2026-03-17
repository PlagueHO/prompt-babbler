import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { RecordingIndicator } from '@/components/recording/RecordingIndicator';

describe('RecordingIndicator', () => {
  it('renders hint text when not recording', () => {
    render(
      <RecordingIndicator
        isRecording={false}
        duration={0}
      />
    );
    expect(screen.getByText(/press to start recording/i)).toBeInTheDocument();
  });

  it('renders status and timer when recording', () => {
    render(
      <RecordingIndicator
        isRecording={true}
        duration={5}
      />
    );
    expect(screen.getByText('Recording')).toBeInTheDocument();
    expect(screen.getByText('00:05')).toBeInTheDocument();
  });
});
