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

export interface MealAnalysisResult {
  score: number;
  completeness: string;
  missing: string[];
  suggestions: string[];
}

export const aiService = {
  async analyzeMeal(mealType: string, items: MealAnalysisItem[]): Promise<MealAnalysisResult> {
    const response = await api.post<MealAnalysisResult>('/ai/analyze-meal', { mealType, items });
    return response.data;
  },
};
