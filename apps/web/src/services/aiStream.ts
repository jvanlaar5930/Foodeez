import { getAuthToken } from './api';

/** One frame on the wire, matching the API's AIStreamEvent. */
interface StreamFrame<T> {
  type: 'delta' | 'result' | 'error' | 'progress' | 'part';
  text?: string;
  data?: T;
  message?: string;
}

/**
 * Work that arrives in pieces reports two more kinds of frame: `progress`, saying where it has
 * got to, and `part`, carrying a finished piece that is already saved. A meal plan uses both,
 * so a week fills the calendar in as it is written instead of appearing all at once at the end.
 *
 * Both are optional. A caller that ignores them sees exactly the behaviour it saw before.
 */
export interface StreamHandlers<TProgress = unknown, TPart = unknown> {
  onProgress?: (progress: TProgress) => void;
  onPart?: (part: TPart) => void;
}

/** A stream that ended without a usable result - `message` is meant to be shown as-is. */
export class AIStreamError extends Error {}

export interface StreamRequest {
  /** Query string values appended to the path. */
  params?: Record<string, string | number | boolean>;
  /** JSON request body, when the endpoint takes one. */
  body?: unknown;
  /** Aborting closes the connection, which is what actually stops the model call. */
  signal?: AbortSignal;
}

/**
 * Reads a server-sent event stream from the API, handing each piece of text to `onDelta` as
 * it arrives and returning the final result.
 *
 * Uses fetch rather than axios: axios buffers the whole response before resolving, which
 * would defeat the point, and EventSource cannot carry the bearer token or a request body.
 */
export async function streamAI<T, TProgress = unknown, TPart = unknown>(
  path: string,
  request: StreamRequest,
  onDelta: (text: string) => void,
  handlers: StreamHandlers<TProgress, TPart> = {},
): Promise<T> {
  const query = request.params
    ? `?${new URLSearchParams(
        Object.entries(request.params).map(([key, value]) => [key, String(value)]),
      )}`
    : '';

  const token = getAuthToken();

  const response = await fetch(`/api${path}${query}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: request.body === undefined ? undefined : JSON.stringify(request.body),
    signal: request.signal,
  });

  if (!response.ok || !response.body) {
    throw new AIStreamError(
      response.status === 400
        ? 'That request could not be analyzed.'
        : 'The AI service could not be reached. Please try again in a moment.',
    );
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let result: T | null = null;
  let failure: string | null = null;

  const handleFrame = (frame: string) => {
    for (const line of frame.split('\n')) {
      if (!line.startsWith('data:')) {
        continue;
      }

      let event: StreamFrame<T>;
      try {
        event = JSON.parse(line.slice(5).trim()) as StreamFrame<T>;
      } catch {
        continue;
      }

      if (event.type === 'delta' && event.text) {
        onDelta(event.text);
      } else if (event.type === 'result' && event.data !== undefined) {
        result = event.data;
      } else if (event.type === 'progress' && event.data !== undefined) {
        handlers.onProgress?.(event.data as unknown as TProgress);
      } else if (event.type === 'part' && event.data !== undefined) {
        handlers.onPart?.(event.data as unknown as TPart);
      } else if (event.type === 'error') {
        failure = event.message ?? 'The AI service could not complete this request.';
      }
    }
  };

  for (;;) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });

    let boundary = buffer.indexOf('\n\n');
    while (boundary >= 0) {
      handleFrame(buffer.slice(0, boundary));
      buffer = buffer.slice(boundary + 2);
      boundary = buffer.indexOf('\n\n');
    }
  }

  // A stream cut off mid-frame leaves the last one unterminated; it may still be the result.
  if (buffer.trim().length > 0) {
    handleFrame(buffer);
  }

  if (failure) {
    throw new AIStreamError(failure);
  }

  if (result === null) {
    throw new AIStreamError('The AI service stopped before finishing. Please try again.');
  }

  return result;
}
