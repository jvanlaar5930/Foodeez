import { onScopeDispose, watch, type Ref } from 'vue';

/**
 * How many things currently want the page behind them to stay put. Counted rather than
 * boolean, because dialogs stack: the recipe panel opens over the meal-slot dialog, and when
 * it closes the dialog underneath still needs the lock.
 */
let holders = 0;

/**
 * Whatever the page's own overflow was before the first lock, so releasing the last one
 * restores it rather than assuming it was the default.
 */
let restore = '';

function acquire(): void {
  if (holders === 0) {
    restore = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
  }
  holders += 1;
}

function release(): void {
  if (holders === 0) {
    return;
  }

  holders -= 1;
  if (holders === 0) {
    document.body.style.overflow = restore;
  }
}

/**
 * Holds the page still while something is open over it, and lets go when it closes or the
 * component goes away.
 *
 * Three components used to write `document.body.style.overflow` directly, each assuming it
 * was the only one. Two consequences, both real: `AppModal` set the lock in a watcher and
 * never released it on unmount, so a dialog torn down while open left the page unscrollable;
 * and `RecipeDetailModal` cleared it as it unmounted even when it had opened over
 * `MealSlotModal`, which is why that dialog carried a hand-written line putting the lock back.
 *
 * @param active Whether this component currently wants the lock.
 */
export function useScrollLock(active: Ref<boolean>): void {
  let held = false;

  function set(wanted: boolean): void {
    if (wanted === held) {
      return;
    }

    held = wanted;
    if (wanted) {
      acquire();
    } else {
      release();
    }
  }

  // Synchronous on purpose. A watcher that waits for the next tick leaves a frame in which
  // the dialog is up and the page behind it still scrolls, which is exactly what someone
  // notices on a phone.
  watch(active, set, { immediate: true, flush: 'sync' });

  // Covers the component being torn down while still open - a route change with a dialog up,
  // or a v-if above it going false.
  onScopeDispose(() => set(false));
}
