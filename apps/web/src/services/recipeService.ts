import api from './api';
import {
  DEFAULT_RECIPE_FILTER_TAGS,
  type Recipe,
  type RecipeQueryFilters,
} from '@foodeez/shared';

/** One page of results plus what the client needs to decide whether to ask for another. */
export interface PagedRecipes {
  items: Recipe[];
  page: number;
  pageSize: number;
  hasMore: boolean;
  /** Null when the source cannot report a total cheaply. */
  totalAvailable: number | null;
}

export const RECIPE_PAGE_SIZE = 10;

/**
 * Coerces a response into a page. The server sends the envelope, but a bare array
 * arrives from an older build - and letting `items` come back undefined strands the
 * view: the computed that reads it throws, and a throwing computed stops Vue
 * committing the DOM, so the page is pinned to its loading state with no way back.
 */
function toPage(data: unknown, page: number, pageSize: number): PagedRecipes {
  if (Array.isArray(data)) {
    return {
      items: data as Recipe[],
      page,
      pageSize,
      // An unpaged source returned everything it had, so a full-looking page is all there is.
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

/** A search-as-you-type suggestion. `recipeId` is present only when the recipe is already stored locally. */
export interface RecipeSuggestion {
  name: string;
  recipeId?: string;
  imageUrl?: string;
}

export const recipeService = {
  /**
   * One page of the library, narrowed by the pills.
   *
   * The filters go to the server rather than being applied to what comes back: filtering the
   * pages already loaded means a pill hides recipes it should show, purely because nobody has
   * scrolled far enough to fetch them yet.
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

  async saveRecipe(id: string): Promise<void> {
    await api.put(`/recipes/${id}/save`);
  },

  async unsaveRecipe(id: string): Promise<void> {
    await api.delete(`/recipes/${id}/save`);
  },

  async getRecipeById(id: string): Promise<Recipe> {
    const response = await api.get<Recipe>(`/recipes/${id}`);
    return response.data;
  },

  /**
   * This user's enhanced version of a recipe, or null if they have never asked for one.
   * A lookup only - it never starts a generation, so opening a recipe costs nothing.
   */
  async getEnhancedRecipe(id: string): Promise<Recipe | null> {
    // 204 with an empty body is how "never enhanced" comes back, which axios hands over as
    // an empty string rather than null.
    const response = await api.get<Recipe | ''>(`/recipes/${id}/enhanced`);
    return response.data ? (response.data as Recipe) : null;
  },

  /**
   * Writes this user's enhanced version of a recipe and returns it.
   *
   * Without `refresh` the server hands back the enhancement they already have rather than
   * writing another, so this is safe to call twice. `refresh` is the reader saying they have
   * seen that one and want a different take on the same dish.
   */
  async enhanceRecipe(id: string, refresh = false): Promise<Recipe> {
    const response = await api.post<Recipe>(`/recipes/${id}/enhance`, null, {
      params: refresh ? { refresh: true } : {},
    });
    return response.data;
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

  /**
   * The filter pills, as an administrator has arranged them. An empty array is a real
   * answer - the pills switched off - so only a failure falls back to the built-in list;
   * showing no filters because a request timed out would look like a broken page.
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

  /**
   * The signed-in user's marked recipes, newest first. The same endpoint the mobile app
   * reads, so a recipe kept on either shows as kept on both.
   */
  async getSavedRecipes(): Promise<Recipe[]> {
    const response = await api.get<Recipe[]>('/recipes/saved');
    return Array.isArray(response.data) ? response.data : [];
  },

  async autocomplete(query: string, signal?: AbortSignal): Promise<RecipeSuggestion[]> {
    const response = await api.get<RecipeSuggestion[]>('/recipes/autocomplete', {
      params: { q: query },
      signal,
    });
    return Array.isArray(response.data) ? response.data : [];
  },
};
