import { api } from './api';
import {
  DEFAULT_RECIPE_FILTER_TAGS,
  type RecipeDto,
  type RecipeQueryFilters,
} from '@/types';

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
  /**
   * One page of the library, narrowed by the pills.
   *
   * The filters go to the server rather than being applied to what comes back: filtering the
   * pages already loaded means a pill hides recipes it should show, purely because the list
   * has not been scrolled far enough to fetch them yet.
   */
  async getRecipes(
    page = 1,
    filters?: RecipeQueryFilters,
    pageSize = RECIPE_PAGE_SIZE,
  ): Promise<PagedRecipes> {
    const response = await api.get<unknown>('/recipes', {
      params: {
        page,
        pageSize,
        ...(filters?.tags.length ? { tags: filters.tags.join(',') } : {}),
        ...(filters?.previousMeals ? { previousMeals: true } : {}),
        ...(filters?.favorites ? { favorites: true } : {}),
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

  /**
   * The filter pills, as an administrator has arranged them. An empty array is a real
   * answer - the pills switched off - so only a failure falls back to the built-in list;
   * showing no filters because a request timed out would look like a broken screen.
   */
  async getFilterTags(): Promise<string[]> {
    try {
      const response = await api.get<unknown>('/recipes/filter-tags');
      if (!Array.isArray(response.data)) return [...DEFAULT_RECIPE_FILTER_TAGS];

      return response.data.filter((tag): tag is string => typeof tag === 'string' && tag.trim() !== '');
    } catch {
      return [...DEFAULT_RECIPE_FILTER_TAGS];
    }
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
