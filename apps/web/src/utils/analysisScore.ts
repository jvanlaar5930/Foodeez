import { scoreBand, scoreColor, type ScoreBand } from '@foodeez/shared';

/**
 * Presentation for an AI score. The thresholds and colours live in @foodeez/shared, because
 * mobile shows the same bands; only the Tailwind mapping below is web's own.
 */

/** A stroke colour for SVG, which cannot take a Tailwind class. */
export function scoreStroke(score: number): string {
  return scoreColor(score);
}

const TEXT_CLASSES: Record<ScoreBand, string> = {
  good: 'text-green-600 dark:text-green-400',
  fair: 'text-amber-600',
  poor: 'text-red-600 dark:text-red-400',
};

const PILL_CLASSES: Record<ScoreBand, string> = {
  good: 'bg-green-100 text-green-700 border-green-200 dark:bg-green-900/40 dark:text-green-300 dark:border-green-800',
  fair: 'bg-amber-100 text-amber-700 border-amber-200 dark:bg-amber-900/40 dark:text-amber-300 dark:border-amber-800',
  poor: 'bg-red-100 text-red-700 border-red-200 dark:bg-red-900/40 dark:text-red-300 dark:border-red-800',
};

export function scoreTextClass(score: number): string {
  return TEXT_CLASSES[scoreBand(score)];
}

/** Background and border for a filled pill carrying the score. */
export function scorePillClass(score: number): string {
  return PILL_CLASSES[scoreBand(score)];
}
