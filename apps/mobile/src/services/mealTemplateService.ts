import { api } from './api';
import type {
  MealTemplateDto,
  QuickAddResultDto,
  RecentMealDto,
  SaveMealTemplateRequest,
} from '@/types';

/**
 * Saved meals and recent meals. Every route reads the owner from the token, so none of these
 * take a user id - the one on SaveMealTemplateRequest is ignored server-side.
 */
export const mealTemplateService = {
  async list(): Promise<MealTemplateDto[]> {
    const response = await api.get<MealTemplateDto[]>('/meal-templates');
    return response.data;
  },

  async recent(): Promise<RecentMealDto[]> {
    const response = await api.get<RecentMealDto[]>('/meal-templates/recent');
    return response.data;
  },

  async save(data: SaveMealTemplateRequest): Promise<MealTemplateDto> {
    const response = await api.post<MealTemplateDto>('/meal-templates', data);
    return response.data;
  },

  /** The saved meal's items, ready to drop into the screen. Also counts the use. */
  async apply(templateId: string): Promise<QuickAddResultDto> {
    const response = await api.post<QuickAddResultDto>(`/meal-templates/${templateId}/apply`);
    return response.data;
  },

  async remove(templateId: string): Promise<void> {
    await api.delete(`/meal-templates/${templateId}`);
  },
};
