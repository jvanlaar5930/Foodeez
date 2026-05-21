import { api } from './api';
import type { DietaryRecommendationsDto } from '@/types';

export async function getDietaryRecommendations(userId: string): Promise<DietaryRecommendationsDto> {
  const response = await api.get<DietaryRecommendationsDto>(`/ai/recommendations/${userId}`);
  return response.data;
}

export async function getUserProfile(userId: string) {
  const response = await api.get(`/users/${userId}/profile`);
  return response.data;
}

export async function updateUserProfile(userId: string, data: Record<string, unknown>) {
  const response = await api.put(`/users/${userId}/profile`, data);
  return response.data;
}
