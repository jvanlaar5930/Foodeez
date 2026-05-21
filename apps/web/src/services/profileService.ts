import api from './api';
import type { UpdateProfileRequest, UserProfile } from '@foodeez/shared';

export const profileService = {
  async getProfile(userId: string): Promise<UserProfile> {
    const response = await api.get<UserProfile>(`/users/${userId}/profile`);
    return response.data;
  },

  async updateProfile(userId: string, data: UpdateProfileRequest): Promise<UserProfile> {
    const response = await api.put<UserProfile>(`/users/${userId}/profile`, data);
    return response.data;
  },
};
