import { create } from 'zustand';
import { api } from '@/services/api';
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
    set({ isLoading: true, error: null });
    try {
      const response = await api.get<UserProfileDto>(`/users/${userId}/profile`);
      set({ profile: response.data, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to fetch profile.';
      set({ isLoading: false, error: message });
    }
  },

  updateProfile: async (userId: string, data: UpdateProfileRequest) => {
    set({ isLoading: true, error: null });
    try {
      const response = await api.put<UserProfileDto>(`/users/${userId}/profile`, data);
      set({ profile: response.data, isLoading: false });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to update profile.';
      set({ isLoading: false, error: message });
      throw err;
    }
  },

  clearError: () => {
    set({ error: null });
  },
}));
