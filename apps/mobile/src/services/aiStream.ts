import { API_URL } from '@/constants/api';

/** One frame on the wire, matching the API's AIStreamEvent. */
interface StreamFrame<T> {
  type: 'delta' | 'result' | 'error';
  text?: string;
  data?: T;
  message?: string;
}

/** A stream that ended without a usable result - `message` is meant to be shown as-is. */
export class AIStreamError extends Error {}

export interface StreamHandle {
  /** Closes the connection, which is what actually stops the model call on the server. */
  cancel: () => void;
}

/**
 * Reads a server-sent event stream from the API, handing each piece of text to `onDelta` as
 * it arrives and resolving with the final result.
 *
 * Uses XMLHttpRequest rather than fetch. React Native's fetch has no streaming response body
 * - `response.body` is undefined and the promise resolves only once the whole response has
 * arrived - which would turn a stream into a long wait followed by everything at once. XHR
 * does report progress, and its `responseText` grows as bytes land, so the new part of it on
 * each progress event is the stream.
 */
export function streamAI<T>(
  path: string,
  options: {
    body?: unknown;
    params?: Record<string, string | number | boolean>;
    token: string | null;
  },
  onDelta: (text: string) => void,
): Promise<T> & StreamHandle {
  const query = options.params
    ? `?${Object.entries(options.params)
        .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
        .join('&')}`
    : '';

  const xhr = new XMLHttpRequest();

  const promise = new Promise<T>((resolve, reject) => {
    // How much of responseText has already been turned into events. Everything before this
    // has been handled; everything after it is what just arrived.
    let consumed = 0;
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
        } else if (event.type === 'error') {
          failure = event.message ?? 'The AI service could not complete this request.';
        }
      }
    };

    const drain = () => {
      const text = xhr.responseText ?? '';
      let buffer = text.slice(consumed);

      // Only whole frames are handled; a frame cut in half by a packet boundary waits for
      // the rest of itself rather than being parsed as broken JSON and dropped.
      let boundary = buffer.indexOf('\n\n');
      while (boundary >= 0) {
        handleFrame(buffer.slice(0, boundary));
        consumed += boundary + 2;
        buffer = buffer.slice(boundary + 2);
        boundary = buffer.indexOf('\n\n');
      }
    };

    xhr.onreadystatechange = () => {
      // 3 is LOADING: the body is still arriving, and responseText holds what has landed.
      if (xhr.readyState === 3) {
        drain();
      }
    };

    xhr.onload = () => {
      if (xhr.status < 200 || xhr.status >= 300) {
        reject(
          new AIStreamError(
            xhr.status === 400
              ? 'That request could not be completed.'
              : 'The AI service could not be reached. Please try again in a moment.',
          ),
        );
        return;
      }

      drain();

      // A stream cut off mid-frame leaves the last one unterminated; it may still be the result.
      const tail = (xhr.responseText ?? '').slice(consumed);
      if (tail.trim().length > 0) {
        handleFrame(tail);
      }

      if (failure) {
        reject(new AIStreamError(failure));
      } else if (result === null) {
        reject(new AIStreamError('The AI service stopped before finishing. Please try again.'));
      } else {
        resolve(result);
      }
    };

    xhr.onerror = () => {
      reject(new AIStreamError('The AI service could not be reached. Please try again in a moment.'));
    };

    xhr.onabort = () => {
      reject(new AIStreamError('Stopped.'));
    };

    xhr.open('POST', `${API_URL}${path}${query}`);
    xhr.setRequestHeader('Content-Type', 'application/json');
    if (options.token) {
      xhr.setRequestHeader('Authorization', `Bearer ${options.token}`);
    }
    xhr.send(options.body === undefined ? null : JSON.stringify(options.body));
  });

  return Object.assign(promise, { cancel: () => xhr.abort() });
}

/** The bearer token, read the same way the axios interceptor reads it. */
export function authToken(): string | null {
  // Lazy require, to avoid the circular import between store and service modules.
  const { useAuthStore } = require('@/store/authStore');
  return useAuthStore.getState().token ?? null;
}
