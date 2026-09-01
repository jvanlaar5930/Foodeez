import type { DayAnalysis, MealAnalysis } from '@foodeez/shared';

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
  /** Analyse a meal that has not been saved yet; the result is persisted when the meal is. */
  async analyzeMeal(mealType: string, items: MealAnalysisItem[]): Promise<MealAnalysisResult> {
    const response = await api.post<MealAnalysisResult>('/ai/analyze-meal', { mealType, items });
    return response.data;
  },

  /**
   * Analyse a saved meal. The server stores the result and serves it back unchanged until the
   * meal's items change, so only `refresh` costs another AI call.
   */
  async analyzeMealLog(mealLogId: string, refresh = false): Promise<MealAnalysisResult> {
    const response = await api.post<MealAnalysisResult>(
      `/meal-logs/${mealLogId}/analysis`,
      null,
      { params: { refresh } },
    );
    return response.data;
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
   * Analyse a whole day. The server stores the result and serves it back until the day's
   * meals or targets change, so only `refresh` costs another AI call.
   */
  async analyzeDay(userId: string, date: string, refresh = false): Promise<DayAnalysis> {
    const response = await api.post<DayAnalysis>('/meal-logs/day-analysis', null, {
      params: { userId, date, refresh },
    });
    return response.data;
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
