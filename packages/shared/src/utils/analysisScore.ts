/**
 * How an AI score is banded and coloured.
 *
 * The same score appears in several places on each client - the badge on a logged meal, the
 * panel inside the meal dialog, the day card - and a meal that reads amber in one and green in
 * another just looks broken. Both apps previously kept their own copy of the thresholds, with
 * mobile's noting in a comment that it "matches apps/web".
 *
 * The thresholds and the colours live here. How a band is *presented* does not: web maps it to
 * Tailwind classes and mobile to StyleSheet values, so each keeps its own mapping keyed on the
 * band this returns.
 */

export type ScoreBand = 'good' | 'fair' | 'poor';

/** A score at or above this is 'good'; at or above FAIR is 'fair'; below that, 'poor'. */
export const SCORE_THRESHOLDS = {
  good: 75,
  fair: 50,
} as const;

export const SCORE_COLORS: Record<ScoreBand, string> = {
  good: '#16a34a',
  fair: '#f59e0b',
  poor: '#ef4444',
};

export function scoreBand(score: number): ScoreBand {
  if (score >= SCORE_THRESHOLDS.good) return 'good';
  if (score >= SCORE_THRESHOLDS.fair) return 'fair';
  return 'poor';
}

/** A raw colour, for an SVG stroke or a StyleSheet value - anywhere a class name cannot go. */
export function scoreColor(score: number): string {
  return SCORE_COLORS[scoreBand(score)];
}
