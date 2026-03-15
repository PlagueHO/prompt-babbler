import { describe, it, expect } from 'vitest';
import { DEFAULT_TEMPLATES } from '@/services/default-templates';

describe('default-templates', () => {
  it('exports three built-in templates', () => {
    expect(DEFAULT_TEMPLATES).toHaveLength(3);
  });

  it('templates have isBuiltIn = true', () => {
    for (const template of DEFAULT_TEMPLATES) {
      expect(template.isBuiltIn).toBe(true);
    }
  });

  it('templates have required fields', () => {
    for (const template of DEFAULT_TEMPLATES) {
      expect(template.id).toBeTruthy();
      expect(template.name).toBeTruthy();
      expect(template.description).toBeTruthy();
      expect(template.systemPrompt).toBeTruthy();
      expect(template.createdAt).toBeTruthy();
      expect(template.updatedAt).toBeTruthy();
    }
  });
});
