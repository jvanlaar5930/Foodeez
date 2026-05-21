import api from './api';
import type { CreateMealPlanRequest, GenerateMealPlanRequest, MealPlan } from '@foodeez/shared';

export const mealPlanService = {
  async getMealPlans(userId: string): Promise<MealPlan[]> {
    const response = await api.get<MealPlan[]>(`/meal-plans`, {
      params: { userId },
    });
    return response.data;
  },

  async getMealPlanById(planId: string): Promise<MealPlan> {
    const response = await api.get<MealPlan>(`/meal-plans/${planId}`);
    return response.data;
  },

  async createMealPlan(data: CreateMealPlanRequest): Promise<MealPlan> {
    const response = await api.post<MealPlan>('/meal-plans', data);
    return response.data;
  },

  async generateAIMealPlan(data: GenerateMealPlanRequest): Promise<MealPlan> {
    const response = await api.post<MealPlan>('/ai/generate-meal-plan', data);
    return response.data;
  },

  async deleteMealPlan(planId: string): Promise<void> {
    await api.delete(`/meal-plans/${planId}`);
  },
};
