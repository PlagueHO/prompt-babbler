import { describe, it, expect } from 'vitest';
import { getTagColor } from '@/lib/tag-colors';

describe('getTagColor', () => {
  it('returns a string containing Tailwind color classes', () => {
    const result = getTagColor('test');
    expect(result).toMatch(/^bg-\w+-\d+ text-\w+-\d+ dark:bg-\w+-\d+ dark:text-\w+-\d+$/);
  });

  it('returns the same color for the same tag', () => {
    const color1 = getTagColor('feature');
    const color2 = getTagColor('feature');
    expect(color1).toBe(color2);
  });

  it('is case-insensitive', () => {
    expect(getTagColor('Bug')).toBe(getTagColor('bug'));
    expect(getTagColor('BUG')).toBe(getTagColor('bug'));
  });

  it('returns different colors for different tags', () => {
    const colors = new Set(['alpha', 'beta', 'gamma', 'delta', 'epsilon'].map(getTagColor));
    expect(colors.size).toBeGreaterThan(1);
  });

  it('handles empty string without throwing', () => {
    expect(() => getTagColor('')).not.toThrow();
    expect(getTagColor('')).toBeTruthy();
  });

  it('handles special characters', () => {
    expect(() => getTagColor('tag-with-dashes')).not.toThrow();
    expect(() => getTagColor('tag with spaces')).not.toThrow();
    expect(() => getTagColor('日本語')).not.toThrow();
  });
});
