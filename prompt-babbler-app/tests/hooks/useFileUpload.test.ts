import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useFileUpload } from '@/hooks/useFileUpload';
import type { Babble } from '@/types';

const baseBabble: Babble = {
  id: 'upload-1',
  title: 'Uploaded Babble',
  text: 'Transcribed audio content.',
  isPinned: false,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
};

vi.mock('@/services/api-client', () => ({
  uploadAudioFile: vi.fn().mockResolvedValue({
    id: 'upload-1',
    title: 'Uploaded Babble',
    text: 'Transcribed audio content.',
    isPinned: false,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
  }),
}));

describe('useFileUpload', () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    const { uploadAudioFile } = await import('@/services/api-client');
    vi.mocked(uploadAudioFile).mockResolvedValue(baseBabble);
  });

  it('returns initial state with isUploading false and no error', () => {
    const { result } = renderHook(() => useFileUpload());

    expect(result.current.isUploading).toBe(false);
    expect(result.current.error).toBeNull();
    expect(typeof result.current.upload).toBe('function');
  });

  it('returns babble on successful upload', async () => {
    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    let babble: Babble | undefined;
    await act(async () => {
      babble = await result.current.upload(file);
    });

    expect(babble).toEqual(baseBabble);
    expect(result.current.isUploading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('calls uploadAudioFile with the file, no title, and auth token', async () => {
    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    await act(async () => {
      await result.current.upload(file);
    });

    const { uploadAudioFile } = await import('@/services/api-client');
    expect(uploadAudioFile).toHaveBeenCalledOnce();
    expect(uploadAudioFile).toHaveBeenCalledWith(file, undefined, 'mock-access-token');
  });

  it('calls uploadAudioFile with the file, provided title, and auth token', async () => {
    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    await act(async () => {
      await result.current.upload(file, 'My Custom Title');
    });

    const { uploadAudioFile } = await import('@/services/api-client');
    expect(uploadAudioFile).toHaveBeenCalledOnce();
    expect(uploadAudioFile).toHaveBeenCalledWith(file, 'My Custom Title', 'mock-access-token');
  });

  it('sets error message on upload failure', async () => {
    const { uploadAudioFile } = await import('@/services/api-client');
    vi.mocked(uploadAudioFile).mockRejectedValueOnce(new Error('Upload failed'));

    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    await act(async () => {
      await result.current.upload(file).catch(() => {});
    });

    expect(result.current.error).toBe('Upload failed');
    expect(result.current.isUploading).toBe(false);
  });

  it('resets error on new upload attempt', async () => {
    const { uploadAudioFile } = await import('@/services/api-client');
    vi.mocked(uploadAudioFile)
      .mockRejectedValueOnce(new Error('First failure'))
      .mockResolvedValueOnce(baseBabble);

    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    // First call fails
    await act(async () => {
      await result.current.upload(file).catch(() => {});
    });
    expect(result.current.error).toBe('First failure');

    // Second call succeeds and clears the error
    await act(async () => {
      await result.current.upload(file);
    });
    expect(result.current.error).toBeNull();
  });

  it('sets isUploading true during upload and false after', async () => {
    let resolveUpload!: (value: Babble) => void;
    const uploadPromise = new Promise<Babble>((resolve) => {
      resolveUpload = resolve;
    });

    const { uploadAudioFile } = await import('@/services/api-client');
    vi.mocked(uploadAudioFile).mockReturnValueOnce(uploadPromise);

    const { result } = renderHook(() => useFileUpload());
    const file = new File(['audio data'], 'test.mp3', { type: 'audio/mpeg' });

    act(() => {
      void result.current.upload(file);
    });

    await waitFor(() => {
      expect(result.current.isUploading).toBe(true);
    });

    await act(async () => {
      resolveUpload(baseBabble);
    });

    await waitFor(() => {
      expect(result.current.isUploading).toBe(false);
    });
  });
});
