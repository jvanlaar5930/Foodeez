import { ref } from 'vue';
import { defineStore } from 'pinia';
import { profileService } from '@/services/profileService';
import { useAsyncState } from '@/stores/asyncState';
import type { UpdateProfileRequest, UserProfile } from '@foodeez/shared';

export const useProfileStore = defineStore('profile', () => {
  const profile = ref<UserProfile | null>(null);
  const { isLoading, error, run, runOrThrow } = useAsyncState();

  async function fetchProfile(userId: string): Promise<void> {
    // Swallowed: the page shows the error and keeps whatever profile it already had.
    const fetched = await run('Your profile could not be loaded.', () =>
      profileService.getProfile(userId),
    );

    if (fetched) {
      profile.value = fetched;
    }
  }

  async function updateProfile(userId: string, data: UpdateProfileRequest): Promise<void> {
    // Rethrown: the caller closes the edit dialog only if the save actually went through.
    profile.value = await runOrThrow('Your profile could not be saved.', () =>
      profileService.updateProfile(userId, data),
    );
  }

  return {
    profile,
    isLoading,
    error,
    fetchProfile,
    updateProfile,
  };
});
