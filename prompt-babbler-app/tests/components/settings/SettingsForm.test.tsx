import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SettingsForm } from '@/components/settings/SettingsForm';

describe('SettingsForm', () => {
  it('renders form fields', () => {
    render(<SettingsForm settings={null} onSave={vi.fn()} />);
    expect(screen.getByLabelText(/azure openai endpoint/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/api key/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/chat deployment name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/whisper deployment name/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save settings/i })).toBeInTheDocument();
  });
});
