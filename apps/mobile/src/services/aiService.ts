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

/** A home-cooked dish described in the cook's own terms. */
export interface EstimateNutritionRequest {
  name: string;
  ingredients?: string;
  servings: number;
  servingDescription?: string;
}

export interface EstimatedNutrition {
  perServing: {
    calories: number;
    protein: number;
    carbohydrates: number;
    fat: number;
    fiber: number;
    sugar: number;
    sodium: number;
  };
  servingSize: number;
  servingUnit: string;
  confidence: 'low' | 'medium' | 'high';
  assumptions?: string;
  assumedIngredients: string[];
  /** False when no estimate could be produced; fall back to manual entry. */
  succeeded: boolean;
}

export async function estimateNutrition(
  data: EstimateNutritionRequest,
): Promise<EstimatedNutrition> {
  const response = await api.post<EstimatedNutrition>('/ai/estimate-nutrition', data);
  return response.data;
}
