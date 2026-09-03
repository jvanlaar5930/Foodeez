/**
 * How an AI score is coloured. Shared because the same score now appears in three places -
 * the badge on a logged meal, the panel inside the meal dialog, and the day card - and a
 * meal that reads amber in one and green in another would just look broken.
 */

/** A stroke colour for SVG, which cannot take a Tailwind class. */
export function scoreStroke(score: number): string {
  if (score >= 75) {
    return '#16a34a';
  }
  if (score >= 50) {
    return '#f59e0b';
  }
  return '#ef4444';
}

export function scoreTextClass(score: number): string {
  if (score >= 75) {
    return 'text-green-600 dark:text-green-400';
  }
  if (score >= 50) {
    return 'text-amber-600';
  }
  return 'text-red-600 dark:text-red-400';
}

/** Background and border for a filled pill carrying the score. */
export function scorePillClass(score: number): string {
  if (score >= 75) {
    return 'bg-green-100 text-green-700 border-green-200 dark:bg-green-900/40 dark:text-green-300 dark:border-green-800';
  }
  if (score >= 50) {
    return 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/40 dark:text-amber-300 dark:border-amber-800';
  }
  return 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/40 dark:text-red-300 dark:border-red-800';
}
