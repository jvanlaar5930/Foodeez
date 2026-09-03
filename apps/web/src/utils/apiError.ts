import { AIStreamError } from '@/services/aiStream';

/**
 * The message worth showing a reader for a failed request.
 *
 * `message` covers the plain-object errors this API returns (the 503 when the AI provider is
 * unavailable, the reasons the meal-plan and grocery endpoints give); detail/title cover the
 * ProblemDetails responses. A stream that ended badly already carries a sentence meant to be
 * shown as-is.
 */
export function extractErrorMessage(err: unknown, fallback = 'An error occurred'): string {
  if (err instanceof AIStreamError) {
    return err.message;
  }

  if (err && typeof err === 'object' && 'response' in err) {
    const e = err as { response?: { data?: { message?: string; detail?: string; title?: string } } };
    return (
      e.response?.data?.message ?? e.response?.data?.detail ?? e.response?.data?.title ?? fallback
    );
  }

  return fallback;
}
