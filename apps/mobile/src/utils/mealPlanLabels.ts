import type { MealLogDto, MealPlanEntryDto } from '@/types';

/**
 * What a planned slot is called on screen.
 *
 * A slot can be a linked recipe, a linked food, or a name somebody typed - and one of those
 * three is what the calendar cell shows. Written out byte-identically, comment included, in
 * both meal-plan screens.
 */
export function entryLabel(entry: MealPlanEntryDto): string {
  return entry.recipeName ?? entry.foodItemName ?? entry.notes ?? 'Custom meal';
}

/** What was actually eaten in a slot, as a list of the foods in it. */
export function loggedLabel(log: MealLogDto): string {
  return log.items
    .map((item) => item.foodItem.name)
    .filter(Boolean)
    .join(', ');
}
