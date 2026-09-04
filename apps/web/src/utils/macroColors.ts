/**
 * The colour each macro is drawn in, in one place.
 *
 * Two palettes existed side by side: the dashboard's macro bars passed hex literals
 * (`#3B82F6`, `#F97316`, `#EAB308`) while the meal dialog's totals used Tailwind classes
 * (`text-blue-600`, `text-orange-500`, `text-amber-500`). Fat was yellow in one and amber in
 * the other, which is the sort of thing nobody notices until the two are on screen together.
 *
 * The `var()` values resolve against Tailwind v4's own palette, so a chart colour and the
 * text class beside it are the same colour rather than two guesses at it.
 */
export const MACRO_COLORS = {
  calories: { swatch: 'var(--color-green-600)', text: 'text-green-600 dark:text-green-400' },
  protein: { swatch: 'var(--color-blue-500)', text: 'text-blue-600 dark:text-blue-400' },
  carbs: { swatch: 'var(--color-orange-500)', text: 'text-orange-500 dark:text-orange-400' },
  fat: { swatch: 'var(--color-amber-500)', text: 'text-amber-500 dark:text-amber-400' },
} as const;

export type Macro = keyof typeof MACRO_COLORS;

/** What "over target" is drawn in, wherever a bar or ring can overshoot. */
export const OVER_TARGET_COLOR = 'var(--color-red-500)';
