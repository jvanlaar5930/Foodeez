import { useCallback, useEffect, useRef, useState } from 'react';
import { analyzeMealLogStream, analyzeMealStream } from '@/services/aiService';
import { AIStreamError } from '@/services/aiStream';
import { nutritionOf, type MealItem } from '@/hooks/useMealItems';
import { MEAL_TYPE_SHORT_LABELS, type MealAnalysisDto, type MealType } from '@/types';

const FAILED = 'The analysis could not be completed. Please try again.';

export interface UseMealAnalysis {
  analysis: MealAnalysisDto | null;
  isAnalyzing: boolean;
  error: string | null;
  /** What the model has written so far, shown while it writes. */
  streamText: string;
  /** The button's wording, which depends on whether there is already a score. */
  label: string;

  run: (input: { userId: string; mealType: MealType; items: MealItem[]; storedMealLogId: string | null }) => void;
  /** Seeds or clears the score - opening the screen on a meal that already has one. */
  set: (analysis: MealAnalysisDto | null) => void;
  /** Drops a score that no longer describes what is on screen. */
  invalidate: () => void;
}

/**
 * Running the AI analysis of a meal, and holding what came back.
 *
 * The cancel-on-unmount is the part that matters: navigating away mid-analysis otherwise
 * leaves the request running and calls setState on a screen that is gone.
 */
export function useMealAnalysis(initial: MealAnalysisDto | null = null): UseMealAnalysis {
  const [analysis, setAnalysis] = useState<MealAnalysisDto | null>(initial);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [streamText, setStreamText] = useState('');

  const inFlight = useRef<{ cancel: () => void } | null>(null);

  useEffect(() => () => inFlight.current?.cancel(), []);

  const invalidate = useCallback(() => {
    setAnalysis(null);
    setError(null);
    setStreamText('');
  }, []);

  /**
   * For a saved meal that has not been touched, the server answers from the score already
   * stored on it - only asking again on an already-scored meal spends a fresh AI call.
   */
  const run = useCallback<UseMealAnalysis['run']>(
    ({ userId, mealType, items, storedMealLogId }) => {
      if (isAnalyzing) return;

      const refresh = analysis !== null;

      setIsAnalyzing(true);
      setAnalysis(null);
      setError(null);
      setStreamText('');

      const onDelta = (text: string) => setStreamText((prev) => prev + text);

      const request = storedMealLogId
        ? analyzeMealLogStream(storedMealLogId, refresh, onDelta)
        : analyzeMealStream(
            userId,
            MEAL_TYPE_SHORT_LABELS[mealType] ?? 'Meal',
            items.map((entry) => ({
              name: entry.foodItem.name,
              amount: entry.quantity,
              unit: entry.foodItem.servingUnit,
              ...nutritionOf(entry),
            })),
            onDelta,
          );

      inFlight.current = request;

      request
        .then(setAnalysis)
        .catch((err: unknown) => {
          setError(err instanceof AIStreamError ? err.message : FAILED);
        })
        .finally(() => {
          inFlight.current = null;
          setIsAnalyzing(false);
        });
    },
    [analysis, isAnalyzing],
  );

  const label = isAnalyzing
    ? 'Analyzing...'
    : analysis
      ? 'Re-run AI Analysis'
      : 'AI Meal Analysis';

  return { analysis, isAnalyzing, error, streamText, label, run, set: setAnalysis, invalidate };
}
