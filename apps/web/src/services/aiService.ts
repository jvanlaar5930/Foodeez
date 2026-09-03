import type { DayAnalysis, MealAnalysis } from '@foodeez/shared';

import { streamAI } from './aiStream';
import api from './api';

export interface MealAnalysisItem {
  name: string;
  amount: number;
  unit: string;
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
}

export type MealAnalysisResult = MealAnalysis;

export const aiService = {
  /**
   * Analyse a meal that has not been saved yet: `onDelta` receives the assessment as the model
   * writes it, and the finished analysis is returned at the end for saving with the meal.
   */
  async analyzeMealStream(
    userId: string,
    mealType: string,
    items: MealAnalysisItem[],
    onDelta: (text: string) => void,
  ): Promise<MealAnalysisResult> {
    // userId, not the exclusions themselves: the server reads those from the profile, so a
    // stale client cannot analyse around someone's allergy.
    return streamAI<MealAnalysisResult>('/ai/analyze-meal/stream', { body: { userId, mealType, items } }, onDelta);
  },

  /**
   * Analyse a saved meal, streaming as the model writes. The server stores the result and
   * serves it back unchanged until the meal's items change, so an analysis already on file
   * arrives complete with no deltas and only `refresh` costs another AI call.
   */
  async analyzeMealLogStream(
    mealLogId: string,
    refresh: boolean,
    onDelta: (text: string) => void,
  ): Promise<MealAnalysisResult> {
    return streamAI<MealAnalysisResult>(
      `/meal-logs/${mealLogId}/analysis/stream`,
      { params: { refresh } },
      onDelta,
    );
  },

  /**
   * The analysis already stored for a day, or null when there is none for the day as it now
   * stands. Costs no AI call, so it is safe to ask for on every page load.
   */
  async getDayAnalysis(userId: string, date: string): Promise<DayAnalysis | null> {
    const response = await api.get<DayAnalysis | ''>('/meal-logs/day-analysis', {
      params: { userId, date },
    });
    return response.status === 204 || !response.data ? null : response.data;
  },

  /**
   * Analyse a whole day, streaming as the model writes. As with a single meal, a stored
   * analysis arrives complete with no deltas and only `refresh` costs another AI call.
   */
  async analyzeDayStream(
    userId: string,
    date: string,
    refresh: boolean,
    onDelta: (text: string) => void,
  ): Promise<DayAnalysis> {
    return streamAI<DayAnalysis>('/meal-logs/day-analysis/stream', { params: { userId, date, refresh } }, onDelta);
  },
};

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
