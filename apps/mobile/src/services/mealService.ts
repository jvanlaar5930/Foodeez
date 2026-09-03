import { api } from './api';
import { AI_REQUEST_TIMEOUT } from '@/constants/api';
import type { LogMealRequest, MealLogDto, NutritionSummaryDto, QuickAddResultDto } from '@/types';

export const mealService = {
  async logMeal(data: LogMealRequest): Promise<MealLogDto> {
    const response = await api.post<MealLogDto>('/meal-logs', data);
    return response.data;
  },

  async updateMealLog(mealLogId: string, data: LogMealRequest): Promise<MealLogDto> {
    const response = await api.put<MealLogDto>(`/meal-logs/${mealLogId}`, data);
    return response.data;
  },

  async getDailyLogs(userId: string, date: string): Promise<MealLogDto[]> {
    const response = await api.get<MealLogDto[]>('/meal-logs', {
      params: { userId, date },
    });
    return response.data;
  },

  /** What was actually logged across a date range - used to show logged meals on the calendar. */
  async getLogsRange(userId: string, startDate: string, endDate: string): Promise<MealLogDto[]> {
    const response = await api.get<MealLogDto[]>('/meal-logs/range', {
      params: { userId, startDate, endDate },
    });
    return response.data;
  },

  async getNutritionSummary(userId: string, date: string): Promise<NutritionSummaryDto> {
    const response = await api.get<NutritionSummaryDto>('/meal-logs/nutrition-summary', {
      params: { userId, date },
    });
    return response.data;
  },

  async deleteMealLog(mealLogId: string): Promise<void> {
    await api.delete(`/meal-logs/${mealLogId}`);
  },

  /**
   * Break a described meal into its separate foods. Nothing is logged: the items come back
   * for the user to look over, and each says whether its numbers came from the food database
   * or from the model.
   */
  async quickAdd(userId: string, description: string): Promise<QuickAddResultDto> {
    // This is one blocking AI call, not the streamed kind - and a self-hosted model can
    // take well past the app's ordinary 30s timeout to answer. The default cut the request
    // off while the model was still working, so the reader saw a generic failure for a
    // request that was actually succeeding server-side.
    const response = await api.post<QuickAddResultDto>(
      '/meal-logs/quick-add',
      { userId, description },
      { timeout: AI_REQUEST_TIMEOUT },
    );
    return response.data;
  },

  /** The same, from a photograph of the plate. */
  async parseFoodImage(imageUri: string): Promise<QuickAddResultDto> {
    const formData = new FormData();
    const filename = imageUri.split('/').pop() ?? 'photo.jpg';
    const match = /\.(\w+)$/.exec(filename);
    const type = match ? `image/${match[1]}` : 'image/jpeg';

    formData.append(
      'image',
      {
        uri: imageUri,
        name: filename,
        type,
      } as unknown as Blob,
    );

    const response = await api.post<QuickAddResultDto>('/meal-logs/parse-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: AI_REQUEST_TIMEOUT,
    });
    return response.data;
  },
};

export const {
  quickAdd,
  logMeal,
  updateMealLog,
  getDailyLogs,
  getLogsRange,
  getNutritionSummary,
  deleteMealLog,
  parseFoodImage,
} = mealService;
