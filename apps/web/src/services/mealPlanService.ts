import { streamAI, type StreamHandlers } from './aiStream';
import api from './api';
import type {
  CreateMealPlanRequest,
  GenerateMealPlanRequest,
  MealPlan,
  MealPlanDay,
  MealPlanEntry,
  MealPlanEntryMoveRequest,
  MealPlanEntryMoveResponse,
  MealPlanEntryRequest,
  MealPlanGenerationResult,
  MealPlanProgress,
} from '@foodeez/shared';

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


  /**
   * Generation, streamed a day at a time.
   *
   * `onDelta` receives the rationale as the model writes it, `onProgress` fires once per day
   * with where the run has got to, and `onPart` carries each day's entries as soon as they are
   * saved - so the calendar fills in while the rest of the week is still being written.
   *
   * The result is no longer the plan alone: a week can now come back partly written, and the
   * dates that failed are part of the answer. Aborting the signal closes the connection, which
   * is what stops the work on the server; days already saved stay saved.
   */
  async generateAIMealPlanStream(
    data: GenerateMealPlanRequest,
    onDelta: (text: string) => void,
    handlers: StreamHandlers<MealPlanProgress, MealPlanDay> = {},
    signal?: AbortSignal,
  ): Promise<MealPlanGenerationResult> {
    return streamAI<MealPlanGenerationResult, MealPlanProgress, MealPlanDay>(
      '/meal-plans/generate/stream',
      { body: data, signal },
      onDelta,
      handlers,
    );
  },

  async deleteMealPlan(planId: string): Promise<void> {
    await api.delete(`/meal-plans/${planId}`);
  },

  /** Fills one slot. The API replaces whatever was already in it, so this is also a move. */
  async addEntry(planId: string, entry: MealPlanEntryRequest): Promise<MealPlanEntry> {
    const response = await api.post<MealPlanEntry>(`/meal-plans/${planId}/entries`, entry);
    return response.data;
  },

  async updateEntry(
    planId: string,
    entryId: string,
    entry: MealPlanEntryRequest,
  ): Promise<MealPlanEntry> {
    const response = await api.put<MealPlanEntry>(
      `/meal-plans/${planId}/entries/${entryId}`,
      entry,
    );
    return response.data;
  },

  /**
   * Drags one meal to another day or slot.
   *
   * Not `updateEntry` with two fields changed: that endpoint replaces whatever occupies the
   * destination, which is right when a meal was deliberately chosen for a slot and wrong for a
   * drag. This one swaps, so a misaimed drop costs a second drag rather than a lost meal.
   */
  async moveEntry(
    planId: string,
    entryId: string,
    destination: MealPlanEntryMoveRequest,
  ): Promise<MealPlanEntryMoveResponse> {
    const response = await api.put<MealPlanEntryMoveResponse>(
      `/meal-plans/${planId}/entries/${entryId}/move`,
      destination,
    );
    return response.data;
  },

  async deleteEntry(planId: string, entryId: string): Promise<void> {
    await api.delete(`/meal-plans/${planId}/entries/${entryId}`);
  },
};
