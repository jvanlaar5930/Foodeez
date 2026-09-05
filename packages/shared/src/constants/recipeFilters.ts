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
