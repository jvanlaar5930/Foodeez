import { ref, type Ref } from 'vue';
import { DEFAULT_RECIPE_FILTER_TAGS } from '@foodeez/shared';
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
const filterTags = ref<string[]>([...DEFAULT_RECIPE_FILTER_TAGS]);
let inFlight: Promise<void> | null = null;

export function useRecipeFilterTags(): { filterTags: Ref<string[]> } {
  inFlight ??= recipeService.getFilterTags().then((tags) => {
    filterTags.value = tags;
  });

  return { filterTags };
}
