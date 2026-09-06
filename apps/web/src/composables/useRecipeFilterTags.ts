import { ref, type Ref } from 'vue';
import { DEFAULT_RECIPE_FILTER_TAGS, withSpecialRecipeFilters } from '@foodeez/shared';
import { recipeService } from '@/services/recipeService';

/**
 * The filter pills shown above recipe results, as an administrator has arranged them.
 *
 * Both views that show them had their own hardcoded copy of the list, which is exactly the
 * kind of duplication that drifts once someone can edit it. The fetch is shared and happens
 * once per page load: the list changes when an administrator edits it, not while somebody
 * browses, so re-requesting it per view would only add requests. The built-in defaults are
 * what render until the answer arrives.
 */
// "Previous Meals" and "Favorites" lead the list here rather than being added at each call
// site, so they are present before the fetch answers and cannot be edited away by an
// administrator - they are not tags, and nothing in the settings row governs them.
const filterTags = ref<string[]>(withSpecialRecipeFilters(DEFAULT_RECIPE_FILTER_TAGS));
let inFlight: Promise<void> | null = null;

export function useRecipeFilterTags(): { filterTags: Ref<string[]> } {
  inFlight ??= recipeService.getFilterTags().then((tags) => {
    filterTags.value = withSpecialRecipeFilters(tags);
  });

  return { filterTags };
}
