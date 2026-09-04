// @vitest-environment jsdom
//
// The only test here that needs a document. The rest of the suite covers stores and
// pure functions and runs faster without one, so the environment is set per file.
import { effectScope, ref, type EffectScope } from 'vue';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { useScrollLock } from './useScrollLock';

/**
 * Three components used to set `document.body.style.overflow` directly, each assuming it was
 * the only one. The stacking case is what that broke: the recipe panel opens over the
 * meal-slot dialog, and closing it released a lock the dialog underneath still needed - which
 * is why that dialog carried a hand-written line putting the lock back.
 *
 * The lock count is module-level, exactly as it is in a running app, so every test here tears
 * down what it mounted - a leaked holder would make the next test read someone else's lock.
 */
describe('useScrollLock', () => {
  let mounted: EffectScope[] = [];

  beforeEach(() => {
    document.body.style.overflow = '';
  });

  afterEach(() => {
    mounted.forEach((scope) => scope.stop());
    mounted = [];
  });

  /** Mounts the composable in its own scope, and returns a way to tear that scope down. */
  function mount(open = true) {
    const active = ref(open);
    const scope = effectScope();
    scope.run(() => useScrollLock(active));
    mounted.push(scope);

    return { active, unmount: () => scope.stop() };
  }

  it('holds the page still while something is open', () => {
    mount();

    expect(document.body.style.overflow).toBe('hidden');
  });

  it('does nothing while closed', () => {
    mount(false);

    expect(document.body.style.overflow).toBe('');
  });

  it('lets go when it closes', () => {
    const { active } = mount();

    active.value = false;

    expect(document.body.style.overflow).toBe('');
  });

  it('lets go when the component is torn down while still open', () => {
    // A route change with a dialog up, or a v-if above it going false.
    const { unmount } = mount();

    unmount();

    expect(document.body.style.overflow).toBe('');
  });

  it('a dialog above another does not release the one below', () => {
    const dialog = mount();
    const panelOverIt = mount();

    panelOverIt.unmount();
    expect(document.body.style.overflow).toBe('hidden');

    dialog.unmount();
    expect(document.body.style.overflow).toBe('');
  });

  it('restores whatever the page had before the first lock', () => {
    document.body.style.overflow = 'scroll';

    const { unmount } = mount();
    expect(document.body.style.overflow).toBe('hidden');

    unmount();
    expect(document.body.style.overflow).toBe('scroll');
  });

  it("tearing the same lock down twice does not release someone else's", () => {
    const first = mount();
    const second = mount();

    first.unmount();
    first.unmount();
    expect(document.body.style.overflow).toBe('hidden');

    second.unmount();
    expect(document.body.style.overflow).toBe('');
  });
});
