import { describeApiError } from '@/utils/apiError';

/**
 * The slice of a store's state this touches. Every store has these two, so a helper can be
 * shared across all of them without knowing anything else about their shape.
 */
export interface AsyncSlice {
  isLoading: boolean;
  error: string | null;
}

type SetAsync = (partial: Partial<AsyncSlice>) => void;

export interface RunOptions {
  /** What to say when the failure carries nothing worth showing. */
  fallback: string;
  /**
   * Whether to raise `isLoading` for the duration. False for a background refresh that
   * should not put a spinner over data already on screen.
   */
  loading?: boolean;
  /**
   * Whether the caller needs to react to a failure - a screen that only navigates away on
   * success, say. Otherwise the message in `error` is the whole story.
   */
  rethrow?: boolean;
}

/**
 * The `{isLoading, error}` envelope every store was writing out by hand.
 *
 * Two things it fixes rather than merely shortens.
 *
 * The message: the stores each ended with
 * `err instanceof Error ? err.message : 'Failed to do the thing.'`, repeated in every store.
 * An axios failure *is* an Error, so that branch is always taken, and `err.message` on one is
 * "Request failed with status code 400" - which is what people were being shown instead of
 * the sentence the server had gone to the trouble of writing. `describeApiError` reads the
 * response body, and separates "the server refused" from "the server was never reached".
 *
 * And the flag: several of the copies set `isLoading: false` on the success path and again in
 * the catch, but a few forgot one branch, which is a screen left spinning with no way back.
 */
export async function runAsync<T>(
  set: SetAsync,
  { fallback, loading = true, rethrow = false }: RunOptions,
  fn: () => Promise<T>,
): Promise<T | undefined> {
  if (loading) {
    set({ isLoading: true, error: null });
  } else {
    set({ error: null });
  }

  try {
    const result = await fn();
    if (loading) {
      set({ isLoading: false });
    }
    return result;
  } catch (err: unknown) {
    // Lowered here as well as on the success path, so a thrown failure cannot leave a screen
    // spinning forever.
    set({ isLoading: false, error: describeApiError(err, fallback) });

    if (rethrow) {
      throw err;
    }
    return undefined;
  }
}
