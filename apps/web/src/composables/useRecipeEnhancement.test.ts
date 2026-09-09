import { effectScope, nextTick, ref } from 'vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useRecipeEnhancement } from './useRecipeEnhancement';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

vi.mock('@/services/recipeService', () => ({
  recipeService: {
    getEnhancedRecipe: vi.fn(),
    enhanceRecipe: vi.fn(),
  },
}));

const getEnhanced = vi.mocked(recipeService.getEnhancedRecipe);
const enhanceRecipe = vi.mocked(recipeService.enhanceRecipe);

function recipeOf(id: string, name: string, extra: Partial<Recipe> = {}): Recipe {
  return {
    id,
    name,
    instructions: 'Cook it.',
    prepTimeMinutes: 5,
    cookTimeMinutes: 10,
    servings: 2,
    isAIGenerated: false,
    ingredients: [],
    nutritionalInfoPerServing: {
      calories: 400,
      protein: 10,
      carbohydrates: 50,
      fat: 12,
      fiber: 4,
      sugar: 6,
      sodium: 300,
    },
    createdAt: '2026-01-01T00:00:00Z',
    ...extra,
  } as Recipe;
}

/**
 * A recipe has at most one enhanced version, and this is what keeps the button honest about
 * it: pressing it a second time shows what exists rather than writing another, and only an
 * explicit refresh replaces one. The other rule worth pinning is the stale-response guard - a
 * generation takes long enough that the reader can be looking at a different recipe by the
 * time it lands, and dropping the enhanced version of last week's pasta onto tonight's curry
 * would be worse than not having the feature.
 */
describe('useRecipeEnhancement', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getEnhanced.mockResolvedValue(null);
  });

  /** Runs the composable in a scope, since it registers a watcher. */
  function inScope<T>(fn: () => T): T {
    return effectScope().run(fn)!;
  }

  it('looks up an existing enhancement on open without generating one', async () => {
    const pasta = recipeOf('r1', 'Pasta');
    const existing = recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1', isEnhanced: true });
    getEnhanced.mockResolvedValue(existing);

    const { enhanced, showingEnhanced, active } = inScope(() =>
      useRecipeEnhancement(() => pasta, () => true),
    );
    await nextTick();

    expect(getEnhanced).toHaveBeenCalledWith('r1');
    expect(enhanceRecipe).not.toHaveBeenCalled();
    expect(enhanced.value).toEqual(existing);

    // Found, but not forced on the reader: the original is what they opened.
    expect(showingEnhanced.value).toBe(false);
    expect(active.value.id).toBe('r1');
  });

  it('looks up nothing for a reader who cannot enhance', async () => {
    const pasta = recipeOf('r1', 'Pasta');

    inScope(() => useRecipeEnhancement(() => pasta, () => false));
    await nextTick();

    expect(getEnhanced).not.toHaveBeenCalled();
  });

  it('writes an enhancement and shows it', async () => {
    const pasta = recipeOf('r1', 'Pasta');
    const written = recipeOf('e1', 'Pasta', {
      enhancedFromRecipeId: 'r1',
      isEnhanced: true,
      instructions: 'Bloom the garlic.',
    });
    enhanceRecipe.mockResolvedValue(written);

    const { enhance, active, showingEnhanced, isEnhancing } = inScope(() =>
      useRecipeEnhancement(() => pasta, () => true),
    );
    await nextTick();

    await enhance();

    expect(enhanceRecipe).toHaveBeenCalledWith('r1', false);
    expect(showingEnhanced.value).toBe(true);
    expect(active.value.instructions).toBe('Bloom the garlic.');
    expect(isEnhancing.value).toBe(false);
  });

  it('shows the enhancement it already has rather than writing a second one', async () => {
    const pasta = recipeOf('r1', 'Pasta');
    getEnhanced.mockResolvedValue(recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1' }));

    const { enhance, showingEnhanced } = inScope(() =>
      useRecipeEnhancement(() => pasta, () => true),
    );
    await nextTick();

    await enhance();

    expect(enhanceRecipe).not.toHaveBeenCalled();
    expect(showingEnhanced.value).toBe(true);
  });

  it('asks for a different take only when the reader refreshes', async () => {
    const pasta = recipeOf('r1', 'Pasta');
    getEnhanced.mockResolvedValue(recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1' }));
    enhanceRecipe.mockResolvedValue(
      recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1', instructions: 'Another way.' }),
    );

    const { refresh, active } = inScope(() => useRecipeEnhancement(() => pasta, () => true));
    await nextTick();

    await refresh();

    expect(enhanceRecipe).toHaveBeenCalledWith('r1', true);
    expect(active.value.instructions).toBe('Another way.');
  });

  it('reports a failure without losing the recipe on screen', async () => {
    const pasta = recipeOf('r1', 'Pasta');
    enhanceRecipe.mockRejectedValue({
      response: { data: { detail: 'The AI service could not enhance this recipe right now.' } },
    });

    const { enhance, error, active, showingEnhanced } = inScope(() =>
      useRecipeEnhancement(() => pasta, () => true),
    );
    await nextTick();

    await enhance();

    expect(error.value).toContain('could not enhance');
    expect(showingEnhanced.value).toBe(false);
    expect(active.value.id).toBe('r1');
  });

  it('drops an enhancement that arrives after the reader has moved on', async () => {
    const open = ref(recipeOf('r1', 'Pasta'));
    let settle: (value: Recipe) => void = () => {};
    enhanceRecipe.mockReturnValue(new Promise<Recipe>((resolve) => (settle = resolve)));

    const { enhance, enhanced, showingEnhanced } = inScope(() =>
      useRecipeEnhancement(() => open.value, () => true),
    );
    await nextTick();

    const inFlight = enhance();

    // The panel is reused for another recipe while the first is still being written.
    open.value = recipeOf('r2', 'Curry');
    await nextTick();

    settle(recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1' }));
    await inFlight;

    expect(enhanced.value).toBeNull();
    expect(showingEnhanced.value).toBe(false);
  });

  it('forgets the previous recipe when another is opened', async () => {
    const open = ref(recipeOf('r1', 'Pasta'));
    getEnhanced.mockResolvedValueOnce(recipeOf('e1', 'Pasta', { enhancedFromRecipeId: 'r1' }));
    getEnhanced.mockResolvedValueOnce(null);

    const { enhanced, active } = inScope(() =>
      useRecipeEnhancement(() => open.value, () => true),
    );
    await nextTick();
    expect(enhanced.value).not.toBeNull();

    open.value = recipeOf('r2', 'Curry');
    await nextTick();
    await nextTick();

    expect(enhanced.value).toBeNull();
    expect(active.value.id).toBe('r2');
    expect(getEnhanced).toHaveBeenLastCalledWith('r2');
  });

  it('does not switch to an enhanced version that does not exist yet', async () => {
    const pasta = recipeOf('r1', 'Pasta');

    const { show, showingEnhanced } = inScope(() =>
      useRecipeEnhancement(() => pasta, () => true),
    );
    await nextTick();

    show(true);

    expect(showingEnhanced.value).toBe(false);
  });
});
