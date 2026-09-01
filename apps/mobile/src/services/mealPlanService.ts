import { api } from './api';
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
  const response = await api.post<MealPlanDto>('/meal-plans/generate', data);
  return response.data;
}

export async function deleteMealPlan(planId: string): Promise<void> {
  await api.delete(`/meal-plans/${planId}`);
}
