import { ref, type Ref } from 'vue';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

export interface RecipeDetail {
  /** The recipe the detail panel is showing, or null when it is closed. */
  selected: Ref<Recipe | null>;
  /** True while the full record is being fetched for the recipe already on screen. */
  isLoading: Ref<boolean>;

  /** Opens a recipe from a result card, filling in what the card did not carry. */
  open(recipe: Recipe): Promise<void>;
  /** Opens a recipe known only by id - a deep link, or a suggestion that resolved to one. */
  openById(id: string): Promise<void>;
  /** Re-requests the full record for whatever is open. */
  retry(): Promise<void>;
  close(): void;
}

/**
 * The recipe detail panel: what is open, and filling it in.
 *
 * Search results carry no ingredients or steps - the API fills those in on first read - so
 * opening a recipe shows the card's data immediately and swaps in the full record when it
 * arrives. Four copies of that fetch existed across two views, each with its own version of
 * the stale-response guard, which is the part that is easy to get wrong: a slow response
 * landing after the reader has opened something else would otherwise replace what they are
 * looking at with the recipe they left.
 */
export function useRecipeDetail(): RecipeDetail {
  const selected = ref<Recipe | null>(null);
  const isLoading = ref(false);

  /** Fetches the full record for `id`, applying it only if that is still what is open. */
  async function fill(id: string): Promise<void> {
    isLoading.value = true;
    try {
      const full = await recipeService.getRecipeById(id);
      if (selected.value?.id === id) {
        selected.value = full;
      }
    } catch {
      // Keep whatever is on screen. The panel says which parts are missing and offers a retry;
      // blanking it would lose the name and picture the reader already had.
    } finally {
      isLoading.value = false;
    }
  }

  async function open(recipe: Recipe): Promise<void> {
    selected.value = recipe;

    // A recipe that already carries its method and ingredients needs no second request.
    if (recipe.ingredients.length && recipe.instructions) {
      return;
    }

    await fill(recipe.id);
  }

  async function openById(id: string): Promise<void> {
    try {
      selected.value = await recipeService.getRecipeById(id);
    } catch {
      // A stale id just means no panel; whatever list is behind it still stands.
    }
  }

  async function retry(): Promise<void> {
    if (selected.value) {
      await fill(selected.value.id);
    }
  }

  function close(): void {
    selected.value = null;
  }

  return { selected, isLoading, open, openById, retry, close };
}
