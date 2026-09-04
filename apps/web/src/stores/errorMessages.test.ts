import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services/profileService', () => ({
  profileService: {
    getProfile: vi.fn(),
    updateProfile: vi.fn(),
  },
}));

import { profileService } from '@/services/profileService';

import { useProfileStore } from './profile';

/**
 * Characterization tests for the loading/error envelope every store repeats, and for the
 * divergence between the private `extractErrorMessage` copies.
 *
 * The assertions about `message` being ignored describe a bug, not a desired behaviour. They
 * are written down so that consolidating the five copies onto `utils/apiError.ts` is a change
 * with a visible effect, rather than a refactor that silently alters what users are told.
 * When that consolidation happens, the two tests tagged BUG below should be updated to expect
 * the server's actual message.
 */
describe('profile store: async envelope', () => {
  const getProfile = vi.mocked(profileService.getProfile);
  const updateProfile = vi.mocked(profileService.updateProfile);

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.resetAllMocks();
  });

  it('clears any previous error when a new request starts', async () => {
    const store = useProfileStore();

    getProfile.mockRejectedValueOnce({ response: { data: { detail: 'first failure' } } });
    await store.fetchProfile('user-1');
    expect(store.error).toBe('first failure');

    getProfile.mockResolvedValueOnce({ userId: 'user-1' } as never);
    await store.fetchProfile('user-1');
    expect(store.error).toBeNull();
  });

  it('lowers isLoading after success', async () => {
    const store = useProfileStore();
    getProfile.mockResolvedValueOnce({ userId: 'user-1' } as never);

    const pending = store.fetchProfile('user-1');
    expect(store.isLoading).toBe(true);

    await pending;
    expect(store.isLoading).toBe(false);
    expect(store.profile).toEqual({ userId: 'user-1' });
  });

  it('lowers isLoading after failure, so the UI cannot be left spinning', async () => {
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce({ response: { data: { detail: 'nope' } } });

    await store.fetchProfile('user-1');

    expect(store.isLoading).toBe(false);
    expect(store.error).toBe('nope');
  });

  it('swallows a fetch failure but rethrows an update failure, so callers can react', async () => {
    const store = useProfileStore();

    getProfile.mockRejectedValueOnce({ response: { data: { detail: 'read failed' } } });
    await expect(store.fetchProfile('user-1')).resolves.toBeUndefined();

    updateProfile.mockRejectedValueOnce({ response: { data: { detail: 'write failed' } } });
    await expect(store.updateProfile('user-1', {} as never)).rejects.toBeDefined();
    expect(store.error).toBe('write failed');
    expect(store.isLoading).toBe(false);
  });

  it('reads ProblemDetails detail and title', async () => {
    const store = useProfileStore();

    getProfile.mockRejectedValueOnce({ response: { data: { detail: 'the detail' } } });
    await store.fetchProfile('user-1');
    expect(store.error).toBe('the detail');

    getProfile.mockRejectedValueOnce({ response: { data: { title: 'the title' } } });
    await store.fetchProfile('user-1');
    expect(store.error).toBe('the title');
  });

  it('BUG: ignores `message`, hiding the reason an AI request failed', async () => {
    // The API sends { message } for its own errors - including the 503 when a provider is
    // unavailable. utils/apiError.ts reads it; this store's private copy does not, so the
    // reader is told nothing useful. The meal-plan store shows the real sentence for the very
    // same response.
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce({
      response: { data: { message: 'The AI service is unavailable. Please try again.' } },
    });

    await store.fetchProfile('user-1');

    expect(store.error).toBe('An error occurred');
  });

  it('BUG: reports a network error as a generic string', async () => {
    // No `response` at all, so nothing distinguishes "the server is down" from any other
    // failure. describeApiError on mobile already separates these.
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce(Object.assign(new Error('Network Error'), { code: 'ERR_NETWORK' }));

    await store.fetchProfile('user-1');

    expect(store.error).toBe('An error occurred');
  });
});
