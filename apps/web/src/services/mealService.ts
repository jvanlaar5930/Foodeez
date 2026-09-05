import api from './api';
import type {
  LogMealRequest,
  MealLog,
  NutritionReport,
  NutritionSummary,
  QuickAddResult,
} from '@foodeez/shared';

export const mealService = {
  async logMeal(data: LogMealRequest): Promise<MealLog> {
    const response = await api.post<MealLog>('/meal-logs', data);
    return response.data;
  },

  async getDailyLogs(userId: string, date: string): Promise<MealLog[]> {
    const response = await api.get<MealLog[]>('/meal-logs', {
      params: { userId, date },
    });
    return response.data;
  },

  /** What was actually logged across a date range - used to show logged meals on the calendar. */
  async getLogsRange(userId: string, startDate: string, endDate: string): Promise<MealLog[]> {
    const response = await api.get<MealLog[]>('/meal-logs/range', {
      params: { userId, startDate, endDate },
    });
    return response.data;
  },

  async updateMealLog(mealLogId: string, data: LogMealRequest): Promise<MealLog> {
    const response = await api.put<MealLog>(`/meal-logs/${mealLogId}`, data);
    return response.data;
  },

  async getNutritionSummary(userId: string, date: string): Promise<NutritionSummary> {
    const response = await api.get<NutritionSummary>('/meal-logs/nutrition-summary', {
      params: { userId, date },
    });
    return response.data;
  },

  /**
   * A date range added up server-side for the reporting screens. Both ends are inclusive and
   * the API caps the span at a year.
   */
  async getReport(startDate: string, endDate: string): Promise<NutritionReport> {
    const response = await api.get<NutritionReport>('/meal-logs/report', {
      params: { startDate, endDate },
    });
    return response.data;
  },

  async deleteMealLog(mealLogId: string): Promise<void> {
    await api.delete(`/meal-logs/${mealLogId}`);
  },

  /**
   * Break a described meal into its separate foods. Nothing is logged: the items come back
   * for the user to look over, and each one says whether its numbers came from the food
   * database or from the model.
   */
  async quickAdd(userId: string, description: string): Promise<QuickAddResult> {
    const response = await api.post<QuickAddResult>('/meal-logs/quick-add', { userId, description });
    return response.data;
  },

  /** The same, from a photograph of the plate. */
  async parseFoodImage(imageFile: File): Promise<QuickAddResult> {
    const formData = new FormData();
    formData.append('image', imageFile);
    const response = await api.post<QuickAddResult>('/meal-logs/parse-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};
