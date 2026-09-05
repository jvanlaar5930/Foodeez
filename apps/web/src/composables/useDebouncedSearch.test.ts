import { effectScope } from 'vue';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useDebouncedSearch } from './useDebouncedSearch';

/**
 * Four components had their own copy of this, and only two guarded against a slow response
 * landing after a fast one. That is the failure worth pinning: type "chick", then finish
 * typing "chicken salad", and the first search - still in flight - comes back last and
 * replaces the right answer with a stale one.
 */
describe('useDebouncedSearch', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  /** Runs the composable inside a scope, since it registers a dispose hook. */
  function inScope<T>(fn: () => T): T {
    const scope = effectScope();
    return scope.run(fn)!;
  }

  it('waits for typing to stop before searching once', async () => {
    const search = vi.fn().mockResolvedValue(['result']);
    const { onInput, results } = inScope(() => useDebouncedSearch(search, { delayMs: 200 }));

    onInput('chi');
    onInput('chic');
    onInput('chick');

    expect(search).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(200);

    expect(search).toHaveBeenCalledTimes(1);
    expect(search.mock.calls[0][0]).toBe('chick');
    expect(results.value).toEqual(['result']);
  });

  it('does not search for something too short to be worth searching', async () => {
    const search = vi.fn().mockResolvedValue(['result']);
    const { onInput } = inScope(() => useDebouncedSearch(search, { minLength: 2 }));

    onInput('c');
    await vi.advanceTimersByTimeAsync(1000);

    expect(search).not.toHaveBeenCalled();
  });

  it('clears results as soon as the query gets too short', async () => {
    const search = vi.fn().mockResolvedValue(['result']);
    const { onInput, results } = inScope(() => useDebouncedSearch(search, { delayMs: 100 }));

    onInput('chicken');
    await vi.advanceTimersByTimeAsync(100);
    expect(results.value).toEqual(['result']);

    onInput('c');
    expect(results.value).toEqual([]);
  });

  it('a slow earlier search does not overwrite a later one', async () => {
    const resolvers: Array<(value: string[]) => void> = [];
    const search = vi.fn(() => new Promise<string[]>((resolve) => resolvers.push(resolve)));
    const { onInput, results } = inScope(() => useDebouncedSearch(search, { delayMs: 10 }));

    onInput('chick');
    await vi.advanceTimersByTimeAsync(10);

    onInput('chicken salad');
    await vi.advanceTimersByTimeAsync(10);

    // The second search answers first, then the first one finally comes back.
    resolvers[1](['chicken salad']);
    await vi.advanceTimersByTimeAsync(0);
    resolvers[0](['chick']);
    await vi.advanceTimersByTimeAsync(0);

    expect(results.value).toEqual(['chicken salad']);
  });

  it('aborts the request it is replacing', async () => {
    const signals: AbortSignal[] = [];
    const search = vi.fn((_q: string, signal: AbortSignal) => {
      signals.push(signal);
      return new Promise<string[]>(() => {});
    });
    const { onInput } = inScope(() => useDebouncedSearch(search, { delayMs: 10 }));

    onInput('chick');
    await vi.advanceTimersByTimeAsync(10);
    onInput('chicken');
    await vi.advanceTimersByTimeAsync(10);

    expect(signals[0].aborted).toBe(true);
    expect(signals[1].aborted).toBe(false);
  });

  it('a failed search empties the list rather than leaving stale results', async () => {
    const search = vi.fn().mockResolvedValueOnce(['result']).mockRejectedValueOnce(new Error('down'));
    const { onInput, results, isSearching } = inScope(() =>
      useDebouncedSearch(search, { delayMs: 10 }),
    );

    onInput('chicken');
    await vi.advanceTimersByTimeAsync(10);
    expect(results.value).toEqual(['result']);

    onInput('chicken soup');
    await vi.advanceTimersByTimeAsync(10);

    expect(results.value).toEqual([]);
    expect(isSearching.value).toBe(false);
  });

  it('reset drops a search that has not been sent yet', async () => {
    const search = vi.fn().mockResolvedValue(['result']);
    const { onInput, reset } = inScope(() => useDebouncedSearch(search, { delayMs: 200 }));

    onInput('chicken');
    reset();
    await vi.advanceTimersByTimeAsync(500);

    expect(search).not.toHaveBeenCalled();
  });

  it('reset discards a search already in flight', async () => {
    let resolve!: (value: string[]) => void;
    const search = vi.fn(() => new Promise<string[]>((r) => (resolve = r)));
    const { onInput, reset, results } = inScope(() => useDebouncedSearch(search, { delayMs: 10 }));

    onInput('chicken');
    await vi.advanceTimersByTimeAsync(10);

    reset();
    resolve(['too late']);
    await vi.advanceTimersByTimeAsync(0);

    expect(results.value).toEqual([]);
  });
});
