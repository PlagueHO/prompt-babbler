import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setAccessCode, getAccessCode, getAccessStatus, getTemplates } from '@/services/api-client';

// Mock global fetch
const mockFetch = vi.fn();
global.fetch = mockFetch;

function mockJsonResponse(data: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: 'OK',
    headers: new Headers({ 'content-type': 'application/json' }),
    json: () => Promise.resolve(data),
    text: () => Promise.resolve(JSON.stringify(data)),
  };
}

describe('api-client access code', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setAccessCode(null);
  });

  it('setAccessCode and getAccessCode work correctly', () => {
    expect(getAccessCode()).toBeNull();

    setAccessCode('my-code');
    expect(getAccessCode()).toBe('my-code');

    setAccessCode(null);
    expect(getAccessCode()).toBeNull();
  });

  it('includes X-Access-Code header when code is set', async () => {
    setAccessCode('secret123');
    mockFetch.mockResolvedValue(mockJsonResponse([]));

    await getTemplates();

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, init] = mockFetch.mock.calls[0];
    expect(init.headers['X-Access-Code']).toBe('secret123');
  });

  it('omits X-Access-Code header when code is null', async () => {
    setAccessCode(null);
    mockFetch.mockResolvedValue(mockJsonResponse([]));

    await getTemplates();

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, init] = mockFetch.mock.calls[0];
    expect(init.headers['X-Access-Code']).toBeUndefined();
  });

  it('includes X-Access-Code on authenticated requests', async () => {
    setAccessCode('secret123');
    mockFetch.mockResolvedValue(mockJsonResponse([]));

    await getTemplates(false, 'bearer-token');

    expect(mockFetch).toHaveBeenCalledTimes(1);
    const [, init] = mockFetch.mock.calls[0];
    expect(init.headers['X-Access-Code']).toBe('secret123');
    expect(init.headers['Authorization']).toBe('Bearer bearer-token');
  });

  it('getAccessStatus calls the correct endpoint', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse({ accessCodeRequired: true }));

    const result = await getAccessStatus();

    expect(result.accessCodeRequired).toBe(true);
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/config/access-status'),
      expect.anything(),
    );
  });
});
