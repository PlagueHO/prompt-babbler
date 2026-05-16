import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SettingsPage } from '@/pages/SettingsPage';

const mockSetTheme = vi.hoisted(() => vi.fn());
const mockUpdateSettings = vi.hoisted(() => vi.fn().mockResolvedValue(undefined));
const mockStartExport = vi.hoisted(() => vi.fn());
const mockGetExportJob = vi.hoisted(() => vi.fn());
const mockStartImport = vi.hoisted(() => vi.fn());
const mockGetImportJob = vi.hoisted(() => vi.fn());

vi.mock('@/hooks/usePageTitle', () => ({
  usePageTitle: vi.fn(),
}));

vi.mock('@/hooks/useSettings', () => ({
  useSettings: vi.fn(() => ({
    isConnected: true,
    isLoading: false,
    error: null,
    refresh: vi.fn(),
  })),
}));

vi.mock('@/hooks/useUserSettings', () => ({
  useUserSettings: vi.fn(() => ({
    settings: {
      theme: 'system',
      speechLanguage: '',
    },
    loading: false,
    error: null,
    updateSettings: mockUpdateSettings,
  })),
}));

vi.mock('@/hooks/useTheme', () => ({
  useTheme: vi.fn(() => ({
    setTheme: mockSetTheme,
  })),
}));

vi.mock('@/hooks/useAuthToken', () => ({
  useAuthToken: vi.fn(() => vi.fn().mockResolvedValue('mock-token')),
}));

vi.mock('@/services/api-client', () => ({
  startExport: mockStartExport,
  getExportJob: mockGetExportJob,
  downloadExport: vi.fn(),
  startImport: mockStartImport,
  getImportJob: mockGetImportJob,
}));

describe('SettingsPage Data import/export', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    mockStartExport.mockResolvedValue('export-job-1');
    mockGetExportJob.mockResolvedValue({
      id: 'export-job-1',
      jobType: 'Export',
      status: 'Queued',
      createdAt: '2026-01-01T00:00:00.000Z',
      progressPercentage: 0,
      totalItems: 0,
      processedItems: 0,
      overwriteExisting: false,
    });

    mockStartImport.mockResolvedValue('import-job-1');
    mockGetImportJob.mockResolvedValue({
      id: 'import-job-1',
      jobType: 'Import',
      status: 'Queued',
      createdAt: '2026-01-01T00:00:00.000Z',
      progressPercentage: 0,
      totalItems: 0,
      processedItems: 0,
      overwriteExisting: false,
    });
  });

  it('renders data section and vectors checkbox defaults unchecked', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Data')).toBeInTheDocument();

    const vectors = screen.getByLabelText('Include semantic vectors');
    expect(vectors).not.toBeChecked();
  });

  it('starts export with vectors excluded by default', async () => {
    const user = userEvent.setup();
    render(<SettingsPage />);

    await user.click(screen.getByRole('button', { name: 'Start export' }));

    await waitFor(() => {
      expect(mockStartExport).toHaveBeenCalledWith(
        {
          includeBabbles: true,
          includeGeneratedPrompts: true,
          includeUserTemplates: true,
          includeSemanticVectors: false,
        },
        'mock-token',
      );
    });
  });

  it('starts import with selected zip and overwrite option', async () => {
    const user = userEvent.setup();
    render(<SettingsPage />);

    const fileInput = screen.getByLabelText('Overwrite existing records').closest('div')?.previousElementSibling as HTMLInputElement;
    const file = new File(['zip-content'], 'payload.zip', { type: 'application/zip' });
    await user.upload(fileInput, file);

    await user.click(screen.getByLabelText('Overwrite existing records'));
    await user.click(screen.getByRole('button', { name: 'Start import' }));

    await waitFor(() => {
      expect(mockStartImport).toHaveBeenCalledWith(file, true, 'mock-token');
    });
  });
});
