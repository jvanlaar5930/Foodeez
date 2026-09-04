import { onScopeDispose, ref, type Ref } from 'vue';

export interface DebouncedSearch<T> {
  /** The most recent results. Cleared as soon as the query drops below `minLength`. */
  results: Ref<T[]>;
  /** True from the keystroke that starts a search until its results land. */
  isSearching: Ref<boolean>;

  /** Call on every keystroke. Schedules a search, replacing any not yet sent. */
  onInput(query: string): void;
  /** Drops any pending search and clears the results - picking a suggestion, or closing. */
  reset(): void;
}

export interface DebouncedSearchOptions {
  /** How long to wait after the last keystroke. */
  delayMs?: number;
  /** Shorter than this and there is nothing worth searching for. */
  minLength?: number;
}

/**
 * Search-as-you-type: waits for typing to pause, then searches, and keeps the results
 * matching what is on screen now rather than what was on screen when the request left.
 *
 * Four components had their own version of this, at 220, 300, 300 and 400 milliseconds. The
 * delay is worth varying - a suggestion dropdown should feel quicker than a full page of
 * results - so it stays a parameter. What is not worth varying is the token guard: without
 * it, a slow search for "chick" lands after a fast one for "chicken salad" and replaces the
 * right answer with a stale one. Only two of the four had it.
 */
export function useDebouncedSearch<T>(
  search: (query: string, signal: AbortSignal) => Promise<T[]>,
  { delayMs = 300, minLength = 2 }: DebouncedSearchOptions = {},
): DebouncedSearch<T> {
  const results = ref<T[]>([]) as Ref<T[]>;
  const isSearching = ref(false);

  let timer: ReturnType<typeof setTimeout> | undefined;
  let latest = 0;
  let inFlight: AbortController | null = null;

  function reset(): void {
    clearTimeout(timer);
    // Bumping the token retires anything already in flight, so its results are discarded
    // when they arrive rather than landing on a cleared list. Aborting as well means a
    // service that takes the signal stops the request instead of finishing it unread.
    latest += 1;
    inFlight?.abort();
    inFlight = null;
    results.value = [];
    isSearching.value = false;
  }

  function onInput(query: string): void {
    clearTimeout(timer);

    const trimmed = query.trim();
    if (trimmed.length < minLength) {
      reset();
      return;
    }

    isSearching.value = true;
    timer = setTimeout(async () => {
      const token = ++latest;
      inFlight?.abort();
      const controller = new AbortController();
      inFlight = controller;

      try {
        const found = await search(trimmed, controller.signal);
        if (token === latest) {
          results.value = found;
        }
      } catch {
        // Search-as-you-type is a convenience; a failure leaves the field usable as typed.
        if (token === latest) {
          results.value = [];
        }
      } finally {
        if (token === latest) {
          isSearching.value = false;
        }
      }
    }, delayMs);
  }

  onScopeDispose(() => {
    clearTimeout(timer);
    inFlight?.abort();
  });

  return { results, isSearching, onInput, reset };
}
