import { streamAI } from './aiStream';
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

  async generateAIMealPlan(data: GenerateMealPlanRequest, signal?: AbortSignal): Promise<MealPlan> {
    // Generation lives on the meal-plans controller, not the AI one - `/ai/generate-meal-plan`
    // has never existed and every click of AI Generate was a 404.
    //
    // The signal is not cosmetic: aborting closes the connection, which the API surfaces as
    // RequestAborted and passes down to the model call, so a cancel here actually stops the
    // work rather than just hiding it.
    const response = await api.post<MealPlan>('/meal-plans/generate', data, { signal });
    return response.data;
  },

  /**
   * The same generation, streamed: `onDelta` receives the plan's rationale as the model
   * writes it, and the saved plan comes back at the end. Aborting the signal closes the
   * connection, which is what stops the work on the server.
   */
  async generateAIMealPlanStream(
    data: GenerateMealPlanRequest,
    onDelta: (text: string) => void,
    signal?: AbortSignal,
  ): Promise<MealPlan> {
    return streamAI<MealPlan>('/meal-plans/generate/stream', { body: data, signal }, onDelta);
  },

  async deleteMealPlan(planId: string): Promise<void> {
    await api.delete(`/meal-plans/${planId}`);
  },
};
