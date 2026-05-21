import api from './api';
import type { Recipe } from '@foodeez/shared';

export const recipeService = {
  async getRecipes(tags?: string[]): Promise<Recipe[]> {
    const response = await api.get<Recipe[]>('/recipes', {
      params: tags && tags.length > 0 ? { tags: tags.join(',') } : undefined,
    });
    return response.data;
  },

  async getRecipeById(id: string): Promise<Recipe> {
    const response = await api.get<Recipe>(`/recipes/${id}`);
    return response.data;
  },

  async searchRecipes(query: string, tags?: string[]): Promise<Recipe[]> {
    const response = await api.get<Recipe[]>('/recipes/search', {
      params: {
        q: query,
        ...(tags && tags.length > 0 ? { tags: tags.join(',') } : {}),
      },
    });
    return response.data;
  },
};
