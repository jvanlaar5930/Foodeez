import api from './api';
import type { LogMealRequest, MealLog, NutritionSummary, ParsedFoodDto } from '@foodeez/shared';

export const mealService = {
  async logMeal(data: LogMealRequest): Promise<MealLog> {
    const response = await api.post<MealLog>('/meal-logs', data);
    return response.data;
  },

  async getDailyLogs(userId: string, date: string): Promise<MealLog[]> {
    const response = await api.get<MealLog[]>(`/meal-logs/${userId}`, {
      params: { date },
    });
    return response.data;
  },

  async getNutritionSummary(userId: string, date: string): Promise<NutritionSummary> {
    const response = await api.get<NutritionSummary>(`/nutrition/summary/${userId}`, {
      params: { date },
    });
    return response.data;
  },

  async deleteMealLog(mealLogId: string): Promise<void> {
    await api.delete(`/meal-logs/${mealLogId}`);
  },

  async parseFoodImage(imageFile: File): Promise<ParsedFoodDto> {
    const formData = new FormData();
    formData.append('image', imageFile);
    const response = await api.post<ParsedFoodDto>('/ai/parse-food-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};
