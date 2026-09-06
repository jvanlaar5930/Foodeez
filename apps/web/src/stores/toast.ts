import { ref } from 'vue';
import { defineStore } from 'pinia';

export type ToastVariant = 'success' | 'error' | 'info' | 'warning';

export interface Toast {
  id: number;
  variant: ToastVariant;
  message: string;
  title?: string;
  /** How long it stays on screen, in ms. Zero keeps it until it is dismissed by hand. */
  duration: number;
}

export interface ToastOptions {
  title?: string;
  duration?: number;
}

/** Long enough to read a sentence, short enough to stay out of the way. */
const DEFAULT_DURATION = 4000;
/** A failure is worth rereading, and often longer, so it lingers. */
const ERROR_DURATION = 6000;
/** Past this the stack starts covering the page it is reporting on. */
const MAX_VISIBLE = 3;

/**
 * The transient confirmations - "meal logged", "settings saved" - that used to be a local ref
 * and a `setTimeout` in whichever view happened to want one.
 *
 * A toast is for a result the reader would otherwise not see: a dialog that closes on save, a
 * background job that finishes, a change that happens somewhere off screen. Feedback that
 * sits next to the control that caused it - a form's own validation, an error banner the page
 * already renders - stays where it is; saying the same thing twice is worse than once.
 */
export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([]);

  // Outside the reactive state on purpose: timer handles are not something to render, and
  // Pinia would only have to track changes nobody reads.
  const timers = new Map<number, ReturnType<typeof setTimeout>>();
  let nextId = 0;

  function show(variant: ToastVariant, message: string, options: ToastOptions = {}): number {
    const duration =
      options.duration ?? (variant === 'error' ? ERROR_DURATION : DEFAULT_DURATION);

    // The same words twice - a double-tapped button, or a retry that failed the same way -
    // restart the one already up rather than stacking a copy on top of it.
    const existing = toasts.value.find(
      (t) => t.variant === variant && t.message === message && t.title === options.title,
    );

    if (existing) {
      existing.duration = duration;
      arm(existing.id, duration);
      return existing.id;
    }

    const toast: Toast = {
      id: ++nextId,
      variant,
      message,
      title: options.title,
      duration,
    };

    toasts.value.push(toast);

    // The oldest goes first: it has been readable the longest, and the newest is the one the
    // reader just caused.
    while (toasts.value.length > MAX_VISIBLE) {
      dismiss(toasts.value[0].id);
    }

    arm(toast.id, duration);
    return toast.id;
  }

  function success(message: string, options?: ToastOptions): number {
    return show('success', message, options);
  }

  function error(message: string, options?: ToastOptions): number {
    return show('error', message, options);
  }

  function warning(message: string, options?: ToastOptions): number {
    return show('warning', message, options);
  }

  function info(message: string, options?: ToastOptions): number {
    return show('info', message, options);
  }

  function dismiss(id: number): void {
    clearTimer(id);
    toasts.value = toasts.value.filter((t) => t.id !== id);
  }

  function clear(): void {
    for (const toast of toasts.value) {
      clearTimer(toast.id);
    }
    toasts.value = [];
  }

  /** (Re)starts the countdown for one toast. A duration of zero means it waits for a click. */
  function arm(id: number, duration: number): void {
    clearTimer(id);
    if (duration > 0) {
      timers.set(id, setTimeout(() => dismiss(id), duration));
    }
  }

  function clearTimer(id: number): void {
    const timer = timers.get(id);
    if (timer !== undefined) {
      clearTimeout(timer);
      timers.delete(id);
    }
  }

  return { toasts, show, success, error, warning, info, dismiss, clear };
});
