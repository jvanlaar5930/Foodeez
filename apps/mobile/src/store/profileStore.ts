import { create } from 'zustand';
import { profileService } from '@/services/profileService';
import { runAsync } from '@/store/asyncState';
import type { UpdateProfileRequest, UserProfileDto } from '@/types';

interface ProfileState {
  profile: UserProfileDto | null;
  isLoading: boolean;
  error: string | null;
  fetchProfile: (userId: string) => Promise<void>;
  updateProfile: (userId: string, data: UpdateProfileRequest) => Promise<void>;
  clearError: () => void;
}

export const useProfileStore = create<ProfileState>()((set) => ({
  profile: null,
  isLoading: false,
  error: null,

  fetchProfile: async (userId: string) => {
    await runAsync(set, { fallback: 'Your profile could not be loaded.' }, async () => {
      set({ profile: await profileService.get(userId) });
    });
  },

  // Rethrown: the edit screen only navigates back once the save has gone through.
  updateProfile: async (userId: string, data: UpdateProfileRequest) => {
    await runAsync(
      set,
      { fallback: 'Your profile could not be saved.', rethrow: true },
      async () => {
        set({ profile: await profileService.update(userId, data) });
      },
    );
  },

  clearError: () => {
    set({ error: null });
  },
}));
