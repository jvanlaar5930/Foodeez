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
 * The loading/error envelope every store used to repeat by hand, now `useAsyncState`.
 *
 * These began as characterization tests written against five diverging private copies of
 * `extractErrorMessage`, with two of them tagged BUG to record what the reader was told
 * instead of the truth. Both now assert the fixed behaviour: the message the server sent, and
 * a connectivity failure named as one.
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

  it('reads `message`, so the reason an AI request failed reaches the reader', async () => {
    // Was a BUG test: only the meal-plan store's private copy read `message`, so the same
    // 503 explained itself on the plan page and said nothing anywhere else.
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce({
      response: { data: { message: 'The AI service is unavailable. Please try again.' } },
    });

    await store.fetchProfile('user-1');

    expect(store.error).toBe('The AI service is unavailable. Please try again.');
  });

  it('names a connectivity failure as one', async () => {
    // Was a BUG test. A request that never reached the server is not the same as one the
    // server refused, and telling someone "an error occurred" sends them off rechecking a
    // form that was fine.
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce(
      Object.assign(new Error('Network Error'), { code: 'ERR_NETWORK', request: {} }),
    );

    await store.fetchProfile('user-1');

    expect(store.error).toContain("Can't reach the Foodeez server");
  });

  it('falls back to the caller\'s sentence when the body says nothing usable', async () => {
    const store = useProfileStore();
    getProfile.mockRejectedValueOnce({ response: { data: {} } });

    await store.fetchProfile('user-1');

    expect(store.error).toBe('Your profile could not be loaded.');
  });

  it('keeps the profile it already had when a refresh fails', async () => {
    // `run` swallows, so the page shows the message over the data it was already showing
    // rather than blanking it.
    const store = useProfileStore();
    getProfile.mockResolvedValueOnce({ userId: 'user-1' } as never);
    await store.fetchProfile('user-1');

    getProfile.mockRejectedValueOnce({ response: { data: { detail: 'later failure' } } });
    await store.fetchProfile('user-1');

    expect(store.profile).toEqual({ userId: 'user-1' });
    expect(store.error).toBe('later failure');
  });
});
