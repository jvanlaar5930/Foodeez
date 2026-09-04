import { AIStreamError } from '@/services/aiStream';

/** What the API puts in a failure body. Every endpoint returns ProblemDetails. */
interface ApiErrorBody {
  detail?: string;
  title?: string;
  /**
   * An older shape some endpoints used before the API settled on ProblemDetails. Read first
   * so that anything still sending it is not reported as a generic failure.
   */
  message?: string;
}

interface AxiosLikeError {
  response?: { data?: ApiErrorBody };
  request?: unknown;
  code?: string;
  message?: string;
}

function isAxiosLike(err: unknown): err is AxiosLikeError {
  return typeof err === 'object' && err !== null && ('response' in err || 'request' in err);
}

/**
 * The message worth showing a reader for a failed request.
 *
 * The distinction that matters is response vs no response. A request that never reached the
 * server is a connectivity problem, and reporting it as "an error occurred" sends people off
 * rechecking a form that was fine all along. The mobile client has always drawn that line;
 * this is the same one.
 *
 * Five near-copies of this used to live in the stores, and they disagreed: only the meal-plan
 * store read `message`, so an AI provider outage explained itself on the plan page and said
 * nothing anywhere else.
 */
export function extractErrorMessage(err: unknown, fallback = 'An error occurred'): string {
  // A stream that ended badly already carries a sentence meant to be shown as-is.
  if (err instanceof AIStreamError) {
    return err.message;
  }

  if (isAxiosLike(err)) {
    if (!err.response) {
      return "Can't reach the Foodeez server. Check your connection and try again.";
    }

    const body = err.response.data;
    return body?.message ?? body?.detail ?? body?.title ?? fallback;
  }

  return fallback;
}
