import axios from 'axios';
import { API_URL } from '@/constants/api';

/**
 * Turns an API failure into a message worth showing, and logs the detail to the Metro
 * console on the way past.
 *
 * The distinction that matters is response vs no response: a request that never reached the
 * server is a connectivity problem, and reporting it as "wrong password" sends people off
 * rechecking credentials that were fine all along.
 */
export function describeApiError(err: unknown, fallback: string): string {
  if (!axios.isAxiosError(err)) {
    console.warn('[api] unexpected error', err);
    return fallback;
  }

  if (!err.response) {
    console.warn(
      `[api] no response from ${API_URL} (${err.code ?? 'network error'}): ${err.message}`,
    );
    return `Can't reach the Foodeez server at ${API_URL}. Check the API is running and that this device can reach it.`;
  }

  const body = err.response.data as { detail?: string; title?: string } | undefined;
  const detail = body?.detail ?? body?.title;
  console.warn(
    `[api] ${err.response.status} ${err.config?.method?.toUpperCase()} ${err.config?.url}: ${detail ?? err.message}`,
  );

  return detail ?? fallback;
}
