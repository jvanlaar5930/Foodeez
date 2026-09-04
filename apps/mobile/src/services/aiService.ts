import { api } from './api';
import { authToken, streamAI, type StreamHandle } from './aiStream';
import { AI_REQUEST_TIMEOUT } from '@/constants/api';
import type { DayAnalysisDto, DietaryRecommendationsDto, MealAnalysisDto } from '@/types';

export async function getDietaryRecommendations(userId: string): Promise<DietaryRecommendationsDto> {
  // A blocking AI call, not the streamed kind - a self-hosted model can outrun the app's
  // ordinary short timeout.
  const response = await api.get<DietaryRecommendationsDto>(`/ai/recommendations/${userId}`, {
    timeout: AI_REQUEST_TIMEOUT,
  });
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
  const response = await api.post<EstimatedNutrition>('/ai/estimate-nutrition', data, {
    timeout: AI_REQUEST_TIMEOUT,
  });
  return response.data;
}

// -- Analysis -----------------------------------------------------------------
// The server stores what it generates and hands the stored copy back unchanged until the
// meal or the day changes, so asking again is free. Only `refresh` spends another AI call.

/**
 * Analyse one saved meal, streaming as the model writes. An analysis already on file
 * arrives complete with no deltas.
 */
export function analyzeMealLogStream(
  mealLogId: string,
  refresh: boolean,
  onDelta: (text: string) => void,
): Promise<MealAnalysisDto> & StreamHandle {
  return streamAI<MealAnalysisDto>(
    `/meal-logs/${mealLogId}/analysis/stream`,
    { params: { refresh }, token: authToken() },
    onDelta,
  );
}

/**
 * The analysis already stored for a day, or null when there is none for the day as it now
 * stands. Costs no AI call, so it is safe on every load.
 */
export async function getDayAnalysis(userId: string, date: string): Promise<DayAnalysisDto | null> {
  const response = await api.get<DayAnalysisDto | ''>('/meal-logs/day-analysis', {
    params: { userId, date },
  });

  // 204 comes through as an empty body, which is "nothing yet" rather than a failure.
  return response.data === '' || response.data === undefined ? null : response.data;
}

/** Analyse a whole day of meals, streaming as the model writes. */
export function analyzeDayStream(
  userId: string,
  date: string,
  refresh: boolean,
  onDelta: (text: string) => void,
): Promise<DayAnalysisDto> & StreamHandle {
  return streamAI<DayAnalysisDto>(
    '/meal-logs/day-analysis/stream',
    { params: { userId, date, refresh }, token: authToken() },
    onDelta,
  );
}

/** One item in an unsaved meal, in the shape the analysis endpoint reads. */
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

/**
 * Analyse a meal that has not been saved yet, streaming as the model writes. For a meal
 * that is already on the server, prefer `analyzeMealLogStream` - it is free once generated
 * and reused until the meal's items change, where this always spends a fresh AI call.
 */
export function analyzeMealStream(
  userId: string,
  mealType: string,
  items: MealAnalysisItem[],
  onDelta: (text: string) => void,
): Promise<MealAnalysisDto> & StreamHandle {
  // userId, not the exclusions themselves: the server reads those from the profile, so a
  // stale client cannot analyse around someone's allergy.
  return streamAI<MealAnalysisDto>(
    '/ai/analyze-meal/stream',
    { body: { userId, mealType, items }, token: authToken() },
    onDelta,
  );
}
