import { api } from './api';
import type { RecipeDto } from '@/types';

export const recipeService = {
  async getRecipes(tags?: string[]): Promise<RecipeDto[]> {
    const response = await api.get<RecipeDto[]>('/recipes', {
      params: tags && tags.length > 0 ? { tags: tags.join(',') } : undefined,
    });
    return response.data;
  },

  async getRecipeById(id: string): Promise<RecipeDto> {
    const response = await api.get<RecipeDto>(`/recipes/${id}`);
    return response.data;
  },

  async searchRecipes(query: string): Promise<RecipeDto[]> {
    const response = await api.get<RecipeDto[]>('/recipes/search', {
      params: { q: query },
    });
    return response.data;
  },
};
