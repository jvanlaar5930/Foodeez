import api from './api';
import type {
  MealTemplate,
  QuickAddResult,
  RecentMeal,
  SaveMealTemplateRequest,
} from '@foodeez/shared';

/**
 * Saved meals and recent meals. Every route reads the owner from the token, so none of these
 * take a user id - the one on SaveMealTemplateRequest is ignored server-side.
 */
export const mealTemplateService = {
  async list(): Promise<MealTemplate[]> {
    const response = await api.get<MealTemplate[]>('/meal-templates');
    return response.data;
  },

  async recent(): Promise<RecentMeal[]> {
    const response = await api.get<RecentMeal[]>('/meal-templates/recent');
    return response.data;
  },

  async save(data: SaveMealTemplateRequest): Promise<MealTemplate> {
    const response = await api.post<MealTemplate>('/meal-templates', data);
    return response.data;
  },

  /** The saved meal's items, ready to drop into the dialog. Also counts the use. */
  async apply(templateId: string): Promise<QuickAddResult> {
    const response = await api.post<QuickAddResult>(`/meal-templates/${templateId}/apply`);
    return response.data;
  },

  async remove(templateId: string): Promise<void> {
    await api.delete(`/meal-templates/${templateId}`);
  },
};
