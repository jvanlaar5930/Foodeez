/**
 * The stand-in picture for a recipe that came out of the advice tab.
 *
 * A recipe the assistant wrote has no photograph - nobody cooked it and nobody photographed
 * it - and inventing one would misrepresent the dish. What it does have is a provenance
 * worth showing, so the thumbnail is the advice tab's own icon and label.
 *
 * This is a marker, not a fetchable URL: each client draws the icon and text itself with the
 * same pieces the tab uses, which stays sharp at any size, needs no hosting and costs no
 * request. Must match AiRecipeImage.Marker on the API.
 */
export const AI_RECIPE_IMAGE = 'foodeez://ask-ai';

/** True when a recipe's image is the advice-tab marker rather than a real picture. */
export function isAiRecipeImage(imageUrl?: string | null): boolean {
  return imageUrl?.toLowerCase() === AI_RECIPE_IMAGE;
}
