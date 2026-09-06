import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { useSavedRecipesStore } from './savedRecipes';
import { recipeService } from '@/services/recipeService';
import type { Recipe } from '@foodeez/shared';

vi.mock('@/services/recipeService', () => ({
  recipeService: {
    getSavedRecipes: vi.fn(),
    saveRecipe: vi.fn(),
    unsaveRecipe: vi.fn(),
  },
}));

const recipe = { id: 'r1', name: 'Lentil Soup' } as Recipe;

/**
 * The bookmark flips before the network answers, so the two things worth pinning are that a
 * failure puts it back, and that the caller is told which of the three outcomes happened -
 * kept, removed, or neither. The message shown to the reader is written from that answer, and
 * announcing a favourite that was not actually saved is worse than saying nothing.
 */
describe('saved recipes store', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('marks a recipe and reports that it is now kept', async () => {
    vi.mocked(recipeService.saveRecipe).mockResolvedValue(undefined);
    const store = useSavedRecipesStore();

    const result = await store.toggle(recipe);

    expect(result).toBe(true);
    expect(store.isSaved('r1')).toBe(true);
    expect(recipeService.saveRecipe).toHaveBeenCalledWith('r1');
  });

  it('unmarks a recipe already kept, and reports that it is gone', async () => {
    vi.mocked(recipeService.saveRecipe).mockResolvedValue(undefined);
    vi.mocked(recipeService.unsaveRecipe).mockResolvedValue(undefined);
    const store = useSavedRecipesStore();

    await store.toggle(recipe);
    const result = await store.toggle(recipe);

    expect(result).toBe(false);
    expect(store.isSaved('r1')).toBe(false);
    expect(recipeService.unsaveRecipe).toHaveBeenCalledWith('r1');
  });

  it('puts the mark back when the request fails, and reports neither outcome', async () => {
    vi.mocked(recipeService.saveRecipe).mockRejectedValue(new Error('offline'));
    const store = useSavedRecipesStore();

    const result = await store.toggle(recipe);

    expect(result).toBeNull();
    expect(store.isSaved('r1')).toBe(false);
    expect(store.error).not.toBeNull();
  });

  it('restores a removal that failed, rather than losing the bookmark', async () => {
    vi.mocked(recipeService.saveRecipe).mockResolvedValue(undefined);
    vi.mocked(recipeService.unsaveRecipe).mockRejectedValue(new Error('offline'));
    const store = useSavedRecipesStore();

    await store.toggle(recipe);
    const result = await store.toggle(recipe);

    expect(result).toBeNull();
    expect(store.isSaved('r1')).toBe(true);
  });

  it('reads the kept list into ids', async () => {
    vi.mocked(recipeService.getSavedRecipes).mockResolvedValue([recipe]);
    const store = useSavedRecipesStore();

    await store.ensureLoaded();

    expect(store.isSaved('r1')).toBe(true);
    expect(store.hasLoaded).toBe(true);
  });

  it('only reads the kept list once, however many screens ask', async () => {
    vi.mocked(recipeService.getSavedRecipes).mockResolvedValue([]);
    const store = useSavedRecipesStore();

    await store.ensureLoaded();
    await store.ensureLoaded();

    expect(recipeService.getSavedRecipes).toHaveBeenCalledTimes(1);
  });
});
