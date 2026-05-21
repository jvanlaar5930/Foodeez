import { api } from './api';
import type { LogMealRequest, MealLogDto, NutritionSummaryDto, ParsedFoodDto } from '@/types';

export async function logMeal(data: LogMealRequest): Promise<MealLogDto> {
  const response = await api.post<MealLogDto>('/meal-logs', data);
  return response.data;
}

export async function getDailyLogs(userId: string, date: string): Promise<MealLogDto[]> {
  const response = await api.get<MealLogDto[]>(`/meal-logs/${userId}`, {
    params: { date },
  });
  return response.data;
}

export async function getNutritionSummary(userId: string, date: string): Promise<NutritionSummaryDto> {
  const response = await api.get<NutritionSummaryDto>(`/meal-logs/${userId}/summary`, {
    params: { date },
  });
  return response.data;
}

export async function deleteMealLog(mealLogId: string): Promise<void> {
  await api.delete(`/meal-logs/${mealLogId}`);
}

export async function parseFoodImage(imageUri: string): Promise<ParsedFoodDto[]> {
  const formData = new FormData();
  const filename = imageUri.split('/').pop() ?? 'photo.jpg';
  const match = /\.(\w+)$/.exec(filename);
  const type = match ? `image/${match[1]}` : 'image/jpeg';

  formData.append('image', {
    uri: imageUri,
    name: filename,
    type,
  } as unknown as Blob);

  const response = await api.post<ParsedFoodDto[]>('/meal-logs/parse-image', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return response.data;
}
