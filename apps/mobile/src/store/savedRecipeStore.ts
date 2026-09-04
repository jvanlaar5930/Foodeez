import { create } from 'zustand';
import { describeApiError } from '@/utils/apiError';
import { recipeService } from '@/services/recipeService';
import type { RecipeDto } from '@/types';

interface SavedRecipeState {
  recipes: RecipeDto[];
  /** Mirrors `recipes` by id so a card can ask "am I saved?" without scanning the list. */
  savedIds: Set<string>;
  /** Ids with a request in flight, so a card can disable itself mid-toggle. */
  pending: Set<string>;
  isLoading: boolean;
  hasLoaded: boolean;
  error: string | null;

  fetch: () => Promise<void>;
  toggle: (recipe: RecipeDto) => Promise<void>;
  isSaved: (recipeId: string) => boolean;
  clear: () => void;
}

export const useSavedRecipeStore = create<SavedRecipeState>()((set, get) => ({
  recipes: [],
  savedIds: new Set(),
  pending: new Set(),
  isLoading: false,
  hasLoaded: false,
  error: null,

  fetch: async () => {
    set({ isLoading: true, error: null });
    try {
      const recipes = await recipeService.getSaved();
      set({
        recipes,
        savedIds: new Set(recipes.map((r) => r.id)),
        isLoading: false,
        hasLoaded: true,
      });
    } catch (err: unknown) {
      // hasLoaded stays true so the UI settles on an error state rather than a spinner.
      set({
        isLoading: false,
        hasLoaded: true,
        error: describeApiError(err, 'Your saved recipes could not be loaded.'),
      });
    }
  },

  /**
   * Flips the save state optimistically. The list reads as instant that way, and both
   * server calls are idempotent, so the rollback on failure is the only correction needed.
   */
  toggle: async (recipe: RecipeDto) => {
    const { savedIds, recipes, pending } = get();
    if (pending.has(recipe.id)) return;

    const wasSaved = savedIds.has(recipe.id);

    const nextIds = new Set(savedIds);
    const nextPending = new Set(pending).add(recipe.id);

    if (wasSaved) nextIds.delete(recipe.id);
    else nextIds.add(recipe.id);

    set({
      savedIds: nextIds,
      pending: nextPending,
      // A newly saved recipe goes to the front, matching the server's newest-first order.
      recipes: wasSaved
        ? recipes.filter((r) => r.id !== recipe.id)
        : [recipe, ...recipes.filter((r) => r.id !== recipe.id)],
      error: null,
    });

    try {
      if (wasSaved) await recipeService.unsave(recipe.id);
      else await recipeService.save(recipe.id);
    } catch (err: unknown) {
      const current = get();
      const revertedIds = new Set(current.savedIds);

      if (wasSaved) revertedIds.add(recipe.id);
      else revertedIds.delete(recipe.id);

      set({
        savedIds: revertedIds,
        recipes: wasSaved
          ? [recipe, ...current.recipes.filter((r) => r.id !== recipe.id)]
          : current.recipes.filter((r) => r.id !== recipe.id),
        error: describeApiError(err, 'Could not update your saved recipes.'),
      });
    } finally {
      const stillPending = new Set(get().pending);
      stillPending.delete(recipe.id);
      set({ pending: stillPending });
    }
  },

  isSaved: (recipeId: string) => get().savedIds.has(recipeId),

  clear: () =>
    set({
      recipes: [],
      savedIds: new Set(),
      pending: new Set(),
      hasLoaded: false,
      error: null,
    }),
}));
