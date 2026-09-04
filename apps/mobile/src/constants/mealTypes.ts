import type { Ionicons } from '@expo/vector-icons';
import { MealType } from '@/types';

/**
 * The icon for each slot of the day.
 *
 * Labels and ordering come from `@foodeez/shared` (MEAL_TYPE_LABELS, ORDERED_MEAL_TYPES),
 * because web shows the same words in the same order. Icons do not: these are Ionicons names,
 * which mean nothing to the web app, so they belong here.
 *
 * Typed as an actual icon name rather than `string`, which is what removed the `as any` at
 * the call sites - a typo in one of these is now a compile error instead of a blank square.
 */
export const MEAL_TYPE_ICONS: Record<MealType, keyof typeof Ionicons.glyphMap> = {
  [MealType.Breakfast]: 'sunny-outline',
  [MealType.MorningSnack]: 'cafe-outline',
  [MealType.Lunch]: 'restaurant-outline',
  [MealType.AfternoonSnack]: 'nutrition-outline',
  [MealType.Dinner]: 'moon-outline',
  [MealType.EveningSnack]: 'ice-cream-outline',
};

export const DEFAULT_MEAL_TYPE_ICON: keyof typeof Ionicons.glyphMap = 'restaurant-outline';
