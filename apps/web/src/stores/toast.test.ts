import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { useToastStore } from './toast';

/**
 * The behaviour that makes a toast a toast rather than a message in an array: it leaves on
 * its own, it does not pile up, and a repeat of the same words does not become two of them.
 */
describe('toast store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows a toast and takes it away once its time is up', () => {
    const toasts = useToastStore();

    toasts.success('Meal logged.');
    expect(toasts.toasts).toHaveLength(1);

    vi.advanceTimersByTime(3999);
    expect(toasts.toasts).toHaveLength(1);

    vi.advanceTimersByTime(1);
    expect(toasts.toasts).toHaveLength(0);
  });

  it('leaves a failure up for longer than a confirmation', () => {
    const toasts = useToastStore();

    toasts.error('That meal could not be saved.');

    vi.advanceTimersByTime(4000);
    expect(toasts.toasts).toHaveLength(1);

    vi.advanceTimersByTime(2000);
    expect(toasts.toasts).toHaveLength(0);
  });

  it('restarts the one on screen rather than stacking the same message twice', () => {
    const toasts = useToastStore();

    const first = toasts.success('Meal logged.');
    vi.advanceTimersByTime(3000);
    const second = toasts.success('Meal logged.');

    expect(second).toBe(first);
    expect(toasts.toasts).toHaveLength(1);

    // Had the timer not been restarted, the original one would go at 4000ms.
    vi.advanceTimersByTime(1500);
    expect(toasts.toasts).toHaveLength(1);

    vi.advanceTimersByTime(2500);
    expect(toasts.toasts).toHaveLength(0);
  });

  it('keeps the newest few and drops the oldest', () => {
    const toasts = useToastStore();

    toasts.success('One.');
    toasts.success('Two.');
    toasts.success('Three.');
    toasts.success('Four.');

    expect(toasts.toasts.map((t) => t.message)).toEqual(['Two.', 'Three.', 'Four.']);
  });

  it('dismisses by hand without the timer removing something else later', () => {
    const toasts = useToastStore();

    const id = toasts.info('Working on it.');
    toasts.dismiss(id);
    expect(toasts.toasts).toHaveLength(0);

    toasts.success('Meal logged.');
    // The dismissed toast's timer would have fired in here; it must not touch this one.
    vi.advanceTimersByTime(3000);
    expect(toasts.toasts).toHaveLength(1);
  });

  it('keeps a toast with no duration until it is dismissed', () => {
    const toasts = useToastStore();

    toasts.show('warning', 'Your plan is half written.', { duration: 0 });

    vi.advanceTimersByTime(60_000);
    expect(toasts.toasts).toHaveLength(1);

    toasts.clear();
    expect(toasts.toasts).toHaveLength(0);
  });
});
