import { api } from './api';
import { AI_REQUEST_TIMEOUT } from '@/constants/api';
import type { GenerateMealPlanRequest, MealPlanDto } from '@/types';

export async function getMealPlans(userId: string): Promise<MealPlanDto[]> {
  // The controller takes userId from the query string; `/meal-plans/{userId}` matches no
  // route at all and was returning 404 on every load.
  const response = await api.get<MealPlanDto[]>('/meal-plans', { params: { userId } });
  return Array.isArray(response.data) ? response.data : [];
}

export async function getMealPlanById(planId: string): Promise<MealPlanDto> {
  const response = await api.get<MealPlanDto>(`/meal-plans/detail/${planId}`);
  return response.data;
}

export async function createMealPlan(data: {
  userId: string;
  name: string;
  startDate: string;
  endDate: string;
}): Promise<MealPlanDto> {
  const response = await api.post<MealPlanDto>('/meal-plans', data);
  return response.data;
}

export async function generateAIMealPlan(data: GenerateMealPlanRequest): Promise<MealPlanDto> {
  // A week's worth of days and meals for the model to reason through routinely takes well
  // past the app's ordinary 30s timeout to answer - same reasoning as the other AI calls.
  const response = await api.post<MealPlanDto>('/meal-plans/generate', data, {
    timeout: AI_REQUEST_TIMEOUT,
  });
  return response.data;
}

export async function deleteMealPlan(planId: string): Promise<void> {
  await api.delete(`/meal-plans/${planId}`);
}
