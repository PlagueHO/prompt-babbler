import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { toast } from 'sonner';
import { TemplatesPage } from '@/pages/TemplatesPage';
import * as api from '@/services/api-client';
import type { PromptTemplate } from '@/types';

vi.mock('sonner', () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

vi.mock('@/services/api-client', () => ({
  listTemplates: vi.fn(),
  getTemplates: vi.fn(),
  createTemplate: vi.fn(),
  updateTemplate: vi.fn(),
  deleteTemplate: vi.fn(),
}));

const customTemplate: PromptTemplate = {
  id: 'custom-1',
  name: 'Custom template',
  description: 'A custom template',
  instructions: 'Summarize the babble.',
  isBuiltIn: false,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
};

describe('TemplatesPage saves', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.stubGlobal('IntersectionObserver', class {
      observe() {}
      disconnect() {}
    });
    vi.stubGlobal('ResizeObserver', class {
      observe() {}
      unobserve() {}
      disconnect() {}
    });
    vi.mocked(api.listTemplates).mockResolvedValue({ items: [customTemplate], continuationToken: null });
    vi.mocked(api.getTemplates).mockResolvedValue([customTemplate]);
    vi.mocked(api.createTemplate).mockResolvedValue(customTemplate);
    vi.mocked(api.updateTemplate).mockResolvedValue(customTemplate);
  });

  afterEach(() => vi.unstubAllGlobals());

  async function openEditor(action: 'create' | 'update') {
    const user = userEvent.setup();
    const view = render(<TemplatesPage />);
    await user.click(await screen.findByRole('button', {
      name: action === 'create' ? 'Create Template' : /Custom template/,
    }));
    if (action === 'create') {
      await user.type(screen.getByLabelText('Name'), customTemplate.name);
      await user.type(screen.getByLabelText('Description'), customTemplate.description);
      await user.type(screen.getByLabelText('Instructions'), customTemplate.instructions);
    }
    return { user, ...view };
  }

  it('lists a minimally populated template after creation and after remounting', async () => {
    vi.mocked(api.listTemplates).mockResolvedValueOnce({ items: [], continuationToken: null });
    const { user, unmount } = await openEditor('create');

    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('button', { name: /Custom template/ })).toBeInTheDocument();
    expect(toast.success).toHaveBeenCalledWith('Template created');
    expect(api.createTemplate).toHaveBeenCalledWith(expect.objectContaining({
      name: customTemplate.name,
      description: customTemplate.description,
      instructions: customTemplate.instructions,
    }), 'mock-access-token');

    unmount();
    render(<TemplatesPage />);

    expect(await screen.findByRole('button', { name: /Custom template/ })).toBeInTheDocument();
  });

  it.each(['create', 'update'] as const)('preserves values and allows retry after a failed %s', async (action) => {
    const mutation = vi.mocked(action === 'create' ? api.createTemplate : api.updateTemplate);
    mutation.mockRejectedValueOnce(new Error('Template validation failed'));
    const { user } = await openEditor(action);
    await user.type(screen.getByLabelText('Instructions'), ' Keep it concise.');

    await user.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith('Template validation failed'));
    expect(toast.success).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Name')).toHaveValue(customTemplate.name);
    expect(screen.getByLabelText('Instructions')).toHaveValue(`${customTemplate.instructions} Keep it concise.`);
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled();

    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByRole('button', { name: /Custom template/ })).toBeInTheDocument();
    expect(toast.success).toHaveBeenCalledWith(action === 'create' ? 'Template created' : 'Template updated');
  });

  it('shows list refresh failures separately from a successful save', async () => {
    const { user } = await openEditor('create');
    vi.mocked(api.listTemplates).mockRejectedValueOnce(new Error('Failed to load templates'));

    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Failed to load templates')).toBeInTheDocument();
    expect(toast.success).toHaveBeenCalledWith('Template created');
    expect(toast.error).not.toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Retry' }));

    expect(await screen.findByRole('button', { name: /Custom template/ })).toBeInTheDocument();
    expect(api.createTemplate).toHaveBeenCalledOnce();
  });
});
