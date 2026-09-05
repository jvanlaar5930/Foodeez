import { ref, type Ref } from 'vue';
import { extractErrorMessage } from '@/utils/apiError';

export interface AsyncState {
  /** True while a request started through this state is in flight. */
  isLoading: Ref<boolean>;
  /** The last failure, in words worth showing, or null. */
  error: Ref<string | null>;

  /**
   * Runs a request that the caller does not need to react to. A failure is recorded in
   * `error` and swallowed - the page shows the message and carries on.
   */
  run<T>(fallback: string, fn: () => Promise<T>): Promise<T | undefined>;

  /**
   * Runs a request the caller does need to react to - closing a dialog only if the save
   * worked, say. A failure is recorded in `error` and rethrown.
   */
  runOrThrow<T>(fallback: string, fn: () => Promise<T>): Promise<T>;
}

/**
 * The loading/error envelope every store was writing out by hand.
 *
 * Roughly twenty copies of `isLoading = true; error = null; try { … } catch { error = … }
 * finally { isLoading = false }` existed across the stores, and they differed in ways nobody
 * chose: some cleared the previous error and some did not, some left `isLoading` true on the
 * failure path, and each carried its own idea of how to read a message out of the failure.
 *
 * Whether a failure is rethrown is a real decision - a page that shows an error banner wants
 * it swallowed, a dialog that closes on success needs it thrown - so it is two named methods
 * rather than a flag.
 */
export function useAsyncState(): AsyncState {
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function runOrThrow<T>(fallback: string, fn: () => Promise<T>): Promise<T> {
    isLoading.value = true;
    error.value = null;
    try {
      return await fn();
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, fallback);
      throw err;
    } finally {
      // In a finally, so a thrown failure cannot leave the page spinning forever.
      isLoading.value = false;
    }
  }

  async function run<T>(fallback: string, fn: () => Promise<T>): Promise<T | undefined> {
    try {
      return await runOrThrow(fallback, fn);
    } catch {
      // Already recorded in `error`. The caller asked not to be told twice.
      return undefined;
    }
  }

  return { isLoading, error, run, runOrThrow };
}
