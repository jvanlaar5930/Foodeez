import { api } from './api';
import type { RecipeDto } from '@/types';

/** One page of results plus what the client needs to decide whether to ask for another. */
export interface PagedRecipes {
  items: RecipeDto[];
  page: number;
  pageSize: number;
  hasMore: boolean;
  /** Null when the source cannot report a total cheaply. */
  totalAvailable: number | null;
}

export const RECIPE_PAGE_SIZE = 10;

/**
 * Coerces a response into a page. The server sends the envelope, but a bare array
 * arrives from an older build, and an undefined `items` would blow up the FlatList
 * on render rather than simply showing no results.
 */
function toPage(data: unknown, page: number, pageSize: number): PagedRecipes {
  if (Array.isArray(data)) {
    return {
      items: data as RecipeDto[],
      page,
      pageSize,
      // An unpaged source returned everything it had, so there is nothing more to fetch.
      hasMore: false,
      totalAvailable: data.length,
    };
  }

  const envelope = (data ?? {}) as Partial<PagedRecipes>;
  return {
    items: Array.isArray(envelope.items) ? envelope.items : [],
    page: envelope.page ?? page,
    pageSize: envelope.pageSize ?? pageSize,
    hasMore: envelope.hasMore === true,
    totalAvailable: envelope.totalAvailable ?? null,
  };
}

export const recipeService = {
  async getRecipes(
    page = 1,
    tags?: string[],
    pageSize = RECIPE_PAGE_SIZE,
  ): Promise<PagedRecipes> {
    const response = await api.get<unknown>('/recipes', {
      params: {
        page,
        pageSize,
        ...(tags && tags.length > 0 ? { tags: tags.join(',') } : {}),
      },
    });
    return toPage(response.data, page, pageSize);
  },

  async getRecipeById(id: string): Promise<RecipeDto> {
    const response = await api.get<RecipeDto>(`/recipes/${id}`);
    return response.data;
  },

  /** The signed-in user's saved recipes, most recently saved first. */
  async getSaved(): Promise<RecipeDto[]> {
    const response = await api.get<RecipeDto[]>('/recipes/saved');
    return Array.isArray(response.data) ? response.data : [];
  },

  /** Idempotent on the server, so a double tap is harmless. */
  async save(recipeId: string): Promise<void> {
    await api.put(`/recipes/${recipeId}/save`);
  },

  async unsave(recipeId: string): Promise<void> {
    await api.delete(`/recipes/${recipeId}/save`);
  },

  async searchRecipes(
    query: string,
    page = 1,
    pageSize = RECIPE_PAGE_SIZE,
  ): Promise<PagedRecipes> {
    const response = await api.get<unknown>('/recipes/search', {
      params: { q: query, page, pageSize },
    });
    return toPage(response.data, page, pageSize);
  },
};
