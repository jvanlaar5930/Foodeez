import { api } from './api';
import { AI_REQUEST_TIMEOUT } from '@/constants/api';
import type {
  LogMealRequest,
  MealLogDto,
  NutritionReport,
  NutritionSummaryDto,
  QuickAddResultDto,
} from '@/types';

/**
 * Extension to media type, by table rather than by `image/${extension}`.
 *
 * The camera writes its photos as ".jpg", and "image/jpg" is not a media type: every vision
 * provider the API can be pointed at rejects the request outright, so every photo taken in
 * the app came back as "that could not be read automatically". The name only ever gets us as
 * far as a guess anyway - the server sniffs the bytes and has the last word - so anything not
 * listed here is sent as JPEG, which is what a phone camera produces.
 */
const IMAGE_MIME_TYPES: Record<string, string> = {
  jpg: 'image/jpeg',
  jpeg: 'image/jpeg',
  png: 'image/png',
  gif: 'image/gif',
  webp: 'image/webp',
  heic: 'image/heic',
  heif: 'image/heic',
};

function imageMimeType(filename: string): string {
  const extension = /\.(\w+)$/.exec(filename)?.[1]?.toLowerCase();
  return (extension && IMAGE_MIME_TYPES[extension]) || 'image/jpeg';
}

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

  /**
   * A date range added up server-side for the reports screen. Both ends are inclusive and
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

    formData.append(
      'image',
      {
        uri: imageUri,
        name: filename,
        type: imageMimeType(filename),
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
