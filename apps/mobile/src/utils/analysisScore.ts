/**
 * How an AI score is coloured. Shared because the same score appears in more than one
 * place - the meal analysis card and the day analysis card - and a meal that reads amber in
 * one and green in another would just look broken. Matches apps/web/src/utils/analysisScore.ts.
 */
export function scoreColor(score: number): string {
  if (score >= 75) {
    return '#16a34a';
  }
  if (score >= 50) {
    return '#f59e0b';
  }
  return '#ef4444';
}
