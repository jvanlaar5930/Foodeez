import { ref } from 'vue';
import { defineStore } from 'pinia';
import { extractErrorMessage } from '@/utils/apiError';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

/**
 * The recipes this user has marked.
 *
 * Ids rather than the recipes: every screen that asks already holds the recipe it is drawing
 * and only needs to know whether it is kept. The full list is fetched once and reduced to
 * ids, which is also what the Favorites pill needs to render its pressed state - the pill
 * itself filters on the server.
 *
 * Mirrors the mobile store deliberately, including the optimistic toggle: the same endpoints,
 * the same behaviour, so a recipe kept on a phone reads as kept in a browser.
 */
export const useSavedRecipesStore = defineStore('savedRecipes', () => {
  const savedIds = ref<Set<string>>(new Set());
  const hasLoaded = ref(false);
  /** Ids with a request in flight, so a button can disable itself mid-toggle. */
  const pending = ref<Set<string>>(new Set());
  const error = ref<string | null>(null);

  async function fetch(): Promise<void> {
    try {
      const recipes = await recipeService.getSavedRecipes();
      savedIds.value = new Set(recipes.map((r) => r.id));
      hasLoaded.value = true;
    } catch (err: unknown) {
      // An empty set is honest here - we do not know what was kept. Failing the whole recipe
      // grid over the bookmark state would be worse than drawing every heart as unfilled.
      savedIds.value = new Set();
      error.value = extractErrorMessage(err);
    }
  }

  /** Fetches once per session; safe to call from every screen that shows a save button. */
  async function ensureLoaded(): Promise<void> {
    if (!hasLoaded.value) await fetch();
  }

  function isSaved(recipeId: string): boolean {
    return savedIds.value.has(recipeId);
  }

  /**
   * Flips the save state optimistically, putting it back if the request fails.
   *
   * A bookmark is a button people press and then immediately look at; waiting on the network
   * to redraw it makes the press feel lost, and this endpoint is idempotent either way.
   *
   * Returns the state it ended in - true kept, false removed - or null if the request failed
   * and the flip was undone. The caller needs that to say which of the two things happened,
   * and must not say either when neither did.
   */
  async function toggle(recipe: Recipe): Promise<boolean | null> {
    if (pending.value.has(recipe.id)) return null;

    const wasSaved = savedIds.value.has(recipe.id);
    const next = new Set(savedIds.value);
    if (wasSaved) next.delete(recipe.id);
    else next.add(recipe.id);

    savedIds.value = next;
    pending.value = new Set(pending.value).add(recipe.id);
    error.value = null;

    try {
      if (wasSaved) await recipeService.unsaveRecipe(recipe.id);
      else await recipeService.saveRecipe(recipe.id);
      return !wasSaved;
    } catch (err: unknown) {
      const reverted = new Set(savedIds.value);
      if (wasSaved) reverted.add(recipe.id);
      else reverted.delete(recipe.id);
      savedIds.value = reverted;
      error.value = extractErrorMessage(err);
      return null;
    } finally {
      const stillPending = new Set(pending.value);
      stillPending.delete(recipe.id);
      pending.value = stillPending;
    }
  }

  return { savedIds, hasLoaded, pending, error, fetch, ensureLoaded, isSaved, toggle };
});
