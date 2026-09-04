import { api } from './api';
import type { UpdateProfileRequest, UserProfileDto } from '@/types';

/**
 * The signed-in user's nutritional profile.
 *
 * A service of its own because profileStore was the one store calling `api` directly, and
 * aiService carried a second, untyped copy of these two endpoints that nothing used.
 */
export const profileService = {
  async get(userId: string): Promise<UserProfileDto> {
    const response = await api.get<UserProfileDto>(`/users/${userId}/profile`);
    return response.data;
  },

  async update(userId: string, data: UpdateProfileRequest): Promise<UserProfileDto> {
    const response = await api.put<UserProfileDto>(`/users/${userId}/profile`, data);
    return response.data;
  },
};

export const { get: getProfile, update: updateProfile } = profileService;
