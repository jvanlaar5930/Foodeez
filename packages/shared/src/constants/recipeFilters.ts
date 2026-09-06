/**
 * The recipe filter pills shown above the results.
 *
 * The live list is administrator-managed and comes from `GET /recipes/filter-tags`; this is
 * what a client shows until that answers, and what it falls back to if the request fails.
 * Must match `RecipeFilterTagsUseCase.Defaults` on the API.
 */
export const DEFAULT_RECIPE_FILTER_TAGS: readonly string[] = [
  'Vegetarian',
  'Vegan',
  'High-Protein',
  'Low-Carb',
  'Quick',
  'Gluten-Free',
  'Dairy-Free',
];

/**
 * Meals the assistant wrote, which land in the recipe library as they are generated.
 *
 * "Previous Meals" rather than "AI" because that is what they are to the person reading the
 * list: things already eaten or planned, not a category of technology.
 */
export const RECIPE_FILTER_PREVIOUS_MEALS = 'Previous Meals';

/** Recipes this user has marked. */
export const RECIPE_FILTER_FAVORITES = 'Favorites';

/**
 * The two pills that are not tags.
 *
 * Every other pill matches against a recipe's `tags` string; these two ask a different
 * question of the recipe itself - who wrote it, and whether this user kept it - so they
 * cannot be administrator-managed rows like the rest, and are hardcoded here where both
 * clients read the same two spellings.
 *
 * First in the list, and always present: the library fills up with generated meals faster
 * than with anything else, so "show me only the ones I chose to keep" and "show me only the
 * ones that arrived on their own" are the two cuts most worth reaching for.
 */
export const SPECIAL_RECIPE_FILTERS: readonly string[] = [
  RECIPE_FILTER_PREVIOUS_MEALS,
  RECIPE_FILTER_FAVORITES,
];

export function isSpecialRecipeFilter(tag: string): boolean {
  return SPECIAL_RECIPE_FILTERS.some((f) => f.toLowerCase() === tag.trim().toLowerCase());
}

/**
 * The pill list a client renders: the two special filters, then the administrator's tags.
 *
 * An administrator tag spelled like one of the specials is dropped rather than shown twice -
 * two identical-looking pills that filter by different rules is worse than losing a tag
 * nobody could have filtered by anyway, since the special one wins the click.
 */
export function withSpecialRecipeFilters(adminTags: readonly string[]): string[] {
  return [...SPECIAL_RECIPE_FILTERS, ...adminTags.filter((tag) => !isSpecialRecipeFilter(tag))];
}

/** The pressed pills, as `GET /recipes` wants them. */
export interface RecipeQueryFilters {
  /** Ordinary tags, matched against the recipe's own tag string by the database. */
  tags: string[];
  /** Only meals the assistant wrote for this user. */
  previousMeals: boolean;
  /** Only recipes this user has marked. */
  favorites: boolean;
}

/**
 * Splits the pressed pills into what the server takes.
 *
 * The two special pills cannot travel as tags: one asks who wrote the recipe and the other
 * whether this user kept it, and sending either as a tag would quietly match nothing while
 * looking like a filter that worked.
 *
 * Shared so both clients send the same request for the same pills - the filtering itself
 * happens in the database, over the whole library rather than the pages already loaded.
 */
export function toRecipeQueryFilters(active: Iterable<string>): RecipeQueryFilters {
  const filters: RecipeQueryFilters = { tags: [], previousMeals: false, favorites: false };

  for (const pill of active) {
    const normalized = pill.trim().toLowerCase();

    if (normalized === RECIPE_FILTER_PREVIOUS_MEALS.toLowerCase()) {
      filters.previousMeals = true;
    } else if (normalized === RECIPE_FILTER_FAVORITES.toLowerCase()) {
      filters.favorites = true;
    } else if (normalized.length > 0) {
      filters.tags.push(pill.trim());
    }
  }

  return filters;
}
