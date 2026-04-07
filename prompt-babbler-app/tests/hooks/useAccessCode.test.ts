import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useAccessCode } from '@/hooks/useAccessCode';

const mockGetAccessStatus = vi.fn();
const mockGetTemplates = vi.fn();
const mockSetAccessCode = vi.fn();

vi.mock('@/services/api-client', () => ({
  getAccessStatus: (...args: unknown[]) => mockGetAccessStatus(...args),
  getTemplates: (...args: unknown[]) => mockGetTemplates(...args),
  setAccessCode: (...args: unknown[]) => mockSetAccessCode(...args),
}));

describe('useAccessCode', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
  });

  it('sets isVerified immediately when access code is not required', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: false });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(true);
    expect(result.current.accessCodeRequired).toBe(false);
  });

  it('validates cached access code from sessionStorage', async () => {
    sessionStorage.setItem('prompt-babbler-access-code', 'cached-code');
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    mockGetTemplates.mockResolvedValue([]);

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(true);
    expect(mockSetAccessCode).toHaveBeenCalledWith('cached-code');
  });

  it('clears invalid cached access code', async () => {
    sessionStorage.setItem('prompt-babbler-access-code', 'bad-code');
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    mockGetTemplates.mockRejectedValue(new Error('API error 401: Unauthorized'));

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.accessCodeRequired).toBe(true);
    expect(sessionStorage.getItem('prompt-babbler-access-code')).toBeNull();
    expect(mockSetAccessCode).toHaveBeenCalledWith(null);
  });

  it('submitCode sets access code and verifies on success', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    mockGetTemplates.mockResolvedValue([]);

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.submitCode('correct-code');
    });

    expect(result.current.isVerified).toBe(true);
    expect(result.current.error).toBeNull();
    expect(sessionStorage.getItem('prompt-babbler-access-code')).toBe('correct-code');
  });

  it('submitCode shows error on 401', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    mockGetTemplates.mockRejectedValue(new Error('API error 401: Unauthorized'));

    await act(async () => {
      await result.current.submitCode('wrong-code');
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.error).toBe('Invalid access code');
  });

  it('submitCode shows network error', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    mockGetTemplates.mockRejectedValue(new Error('Failed to fetch'));

    await act(async () => {
      await result.current.submitCode('some-code');
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.error).toBe('Unable to connect to the server');
  });

  it('falls back to verified when server is unreachable on initial check', async () => {
    mockGetAccessStatus.mockRejectedValue(new Error('Network error'));

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(true);
  });
});
