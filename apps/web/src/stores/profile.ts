import { ref } from 'vue';
import { defineStore } from 'pinia';
import { profileService } from '@/services/profileService';
import type { UpdateProfileRequest, UserProfile } from '@foodeez/shared';

export const useProfileStore = defineStore('profile', () => {
  const profile = ref<UserProfile | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function fetchProfile(userId: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      profile.value = await profileService.getProfile(userId);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
    } finally {
      isLoading.value = false;
    }
  }

  async function updateProfile(userId: string, data: UpdateProfileRequest): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      profile.value = await profileService.updateProfile(userId, data);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  function extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'response' in err) {
      const e = err as { response?: { data?: { detail?: string; title?: string } } };
      return e.response?.data?.detail ?? e.response?.data?.title ?? 'An error occurred';
    }
    return 'An error occurred';
  }

  return {
    profile,
    isLoading,
    error,
    fetchProfile,
    updateProfile,
  };
});
