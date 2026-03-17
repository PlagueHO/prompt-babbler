import { describe, it, expect, beforeEach } from 'vitest';
import {
  getSpeechLanguage,
  setSpeechLanguage,
} from '@/services/local-storage';

describe('local-storage service', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('getSpeechLanguage returns empty string when no data', () => {
    expect(getSpeechLanguage()).toBe('');
  });

  it('setSpeechLanguage stores and retrieves language', () => {
    setSpeechLanguage('en-US');
    expect(getSpeechLanguage()).toBe('en-US');
  });
});
