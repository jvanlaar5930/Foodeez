import { computed, ref, watch, type ComputedRef, type Ref } from 'vue';
import { recipeService } from '@/services/recipeService';
import { extractErrorMessage } from '@/utils/apiError';
import type { Recipe } from '@foodeez/shared';

export interface RecipeEnhancement {
  /** The version on screen: the enhanced one when it is being shown, otherwise the original. */
  active: ComputedRef<Recipe>;
  /** This user's enhanced version, once it has been fetched or written. */
  enhanced: Ref<Recipe | null>;
  /** Whether the enhanced version is the one being shown. */
  showingEnhanced: Ref<boolean>;
  /** True while the model is writing one, which takes a while and needs saying so. */
  isEnhancing: Ref<boolean>;
  /** True while an existing enhancement is being looked up on open. */
  isLoading: Ref<boolean>;
  /** Why the last attempt failed, phrased for the reader. */
  error: Ref<string | null>;

  /** Writes an enhancement, or shows the one that already exists. */
  enhance(): Promise<void>;
  /** Asks for a different take on the same dish, replacing the one being shown. */
  refresh(): Promise<void>;
  show(enhanced: boolean): void;
}

/**
 * The two versions of a recipe: the one that was written, and the one a chef would cook.
 *
 * A recipe has at most one enhancement per person - the server enforces that, and this is
 * what makes the button honest about it. Opening a recipe looks the enhancement up but never
 * writes one, so nobody spends a generation by browsing; pressing "enhance" when one already
 * exists shows it rather than making another; and a refresh is the only thing that replaces
 * it, because that is the reader saying they have seen this one and want something else.
 *
 * Both arguments are getters rather than refs, so a caller can pass a prop straight in
 * without the two ending up as separate pieces of state. `recipe` is watched rather than read
 * once: the detail panel fills in ingredients and steps after it opens, and the same panel is
 * reused for whatever recipe is opened next.
 */
export function useRecipeEnhancement(
  recipe: () => Recipe,
  canEnhance: () => boolean,
): RecipeEnhancement {
  const enhanced = ref<Recipe | null>(null);
  const showingEnhanced = ref(false);
  const isEnhancing = ref(false);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  /** Which recipe the state above belongs to, so a late response cannot land on another. */
  const forRecipeId = ref<string | null>(null);

  const active = computed<Recipe>(() =>
    showingEnhanced.value && enhanced.value ? enhanced.value : recipe(),
  );

  function reset(): void {
    enhanced.value = null;
    showingEnhanced.value = false;
    isEnhancing.value = false;
    error.value = null;
  }

  /**
   * Looks up an existing enhancement for a recipe as it opens.
   *
   * The id is captured and re-checked afterwards: a reader who closes this panel and opens
   * another before the request lands would otherwise be offered the previous recipe's
   * enhanced version, under the new recipe's name.
   */
  async function load(id: string): Promise<void> {
    isLoading.value = true;
    try {
      const existing = await recipeService.getEnhancedRecipe(id);
      if (forRecipeId.value === id) {
        enhanced.value = existing;
      }
    } catch {
      // Not worth a message. Nothing is broken from the reader's point of view - the recipe
      // is on screen and the button simply offers to write one.
    } finally {
      if (forRecipeId.value === id) isLoading.value = false;
    }
  }

  watch(
    // The original's id, not the active recipe's: switching between the two versions is not
    // a new recipe and must not re-fetch anything.
    () => recipe().id,
    (id) => {
      if (id === forRecipeId.value) return;

      forRecipeId.value = id;
      reset();

      // An enhancement belongs to a signed-in reader; there is nothing to look up otherwise.
      if (id && canEnhance()) void load(id);
    },
    { immediate: true },
  );

  /** Writes an enhancement, or - when one already exists - simply shows it. */
  async function enhance(): Promise<void> {
    if (enhanced.value) {
      showingEnhanced.value = true;
      return;
    }

    await write(false);
  }

  async function refresh(): Promise<void> {
    await write(true);
  }

  async function write(replacing: boolean): Promise<void> {
    const id = recipe().id;
    if (!id || isEnhancing.value) return;

    isEnhancing.value = true;
    error.value = null;

    try {
      const result = await recipeService.enhanceRecipe(id, replacing);

      // Still the recipe that was asked about. A generation can take a minute, which is
      // plenty of time to have moved on to another recipe.
      if (forRecipeId.value === id) {
        enhanced.value = result;
        showingEnhanced.value = true;
      }
    } catch (err: unknown) {
      if (forRecipeId.value === id) {
        error.value = extractErrorMessage(
          err,
          'That could not be enhanced just now. Please try again in a moment.',
        );
      }
    } finally {
      if (forRecipeId.value === id) isEnhancing.value = false;
    }
  }

  function show(value: boolean): void {
    // Nothing to switch to until one exists, and the toggle is not drawn before then.
    if (value && !enhanced.value) return;
    showingEnhanced.value = value;
    error.value = null;
  }

  return { active, enhanced, showingEnhanced, isEnhancing, isLoading, error, enhance, refresh, show };
}
