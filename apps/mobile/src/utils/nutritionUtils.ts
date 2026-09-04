/**
 * Display helpers for nutrition figures, in this app's own idiom.
 *
 * The calculations that used to live here - BMR, TDEE, calorie and macro targets, BMI - moved
 * to `@foodeez/shared`, which mirrors the server's own formula. Keeping a second copy here is
 * what let the setup screen preview one set of targets while the server stored another.
 */

/**
 * Format calories with thousands separator and unit.
 */
export function formatCalories(calories: number): string {
  return `${Math.round(calories).toLocaleString()} kcal`;
}

/**
 * Format macro grams value.
 */
export function formatMacro(grams: number): string {
  return `${Math.round(grams)}g`;
}

/**
 * Clamp a percentage value between 0 and 100.
 */
export function clampPercentage(value: number): number {
  return Math.min(100, Math.max(0, value));
}
