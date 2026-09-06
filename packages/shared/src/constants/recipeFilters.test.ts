import { describe, it, expect } from 'vitest';
import {
  DEFAULT_RECIPE_FILTER_TAGS,
  RECIPE_FILTER_FAVORITES,
  RECIPE_FILTER_PREVIOUS_MEALS,
  isSpecialRecipeFilter,
  toRecipeQueryFilters,
  withSpecialRecipeFilters,
} from './recipeFilters';

/**
 * The two pills that are not tags.
 *
 * The filtering itself happens in the database; what is shared here is which pills exist and
 * how a pressed one reaches the query. Both matter: a special pill sent as a tag would match
 * nothing while looking like it worked, and the pair being hardcoded and first is a
 * requirement rather than a default - they must be there whatever an administrator has done
 * to the tag list.
 */
describe('withSpecialRecipeFilters', () => {
  it('puts the two special filters first, ahead of the administrator tags', () => {
    const list = withSpecialRecipeFilters(['Vegan', 'Quick']);

    expect(list).toEqual([RECIPE_FILTER_PREVIOUS_MEALS, RECIPE_FILTER_FAVORITES, 'Vegan', 'Quick']);
  });

  it('still offers them when the administrator has switched every tag off', () => {
    expect(withSpecialRecipeFilters([])).toEqual([
      RECIPE_FILTER_PREVIOUS_MEALS,
      RECIPE_FILTER_FAVORITES,
    ]);
  });

  it('drops an administrator tag that collides with one, rather than showing it twice', () => {
    const list = withSpecialRecipeFilters(['favorites', 'Vegan']);

    expect(list.filter((t) => t.toLowerCase() === 'favorites')).toHaveLength(1);
    expect(list).toContain('Vegan');
  });

  it('leaves the built-in defaults intact behind them', () => {
    const list = withSpecialRecipeFilters(DEFAULT_RECIPE_FILTER_TAGS);

    expect(list.slice(2)).toEqual([...DEFAULT_RECIPE_FILTER_TAGS]);
  });
});

describe('isSpecialRecipeFilter', () => {
  it.each(['Favorites', 'favorites', '  Previous Meals  '])('recognises %s', (tag) => {
    expect(isSpecialRecipeFilter(tag)).toBe(true);
  });

  it('does not claim an ordinary tag', () => {
    expect(isSpecialRecipeFilter('Vegan')).toBe(false);
  });
});

describe('toRecipeQueryFilters', () => {
  it('sends ordinary pills as tags', () => {
    expect(toRecipeQueryFilters(['Vegan', 'Quick'])).toEqual({
      tags: ['Vegan', 'Quick'],
      previousMeals: false,
      favorites: false,
    });
  });

  it('lifts the two special pills out of the tags', () => {
    const filters = toRecipeQueryFilters([
      RECIPE_FILTER_PREVIOUS_MEALS,
      RECIPE_FILTER_FAVORITES,
      'Vegan',
    ]);

    expect(filters).toEqual({ tags: ['Vegan'], previousMeals: true, favorites: true });
  });

  it('never sends a special pill as a tag, which would match nothing', () => {
    const filters = toRecipeQueryFilters([RECIPE_FILTER_FAVORITES]);

    expect(filters.tags).toEqual([]);
    expect(filters.favorites).toBe(true);
  });

  it('recognises them whatever case they arrive in', () => {
    expect(toRecipeQueryFilters(['favorites']).favorites).toBe(true);
    expect(toRecipeQueryFilters(['PREVIOUS MEALS']).previousMeals).toBe(true);
  });

  it('is empty for no pills at all', () => {
    expect(toRecipeQueryFilters([])).toEqual({ tags: [], previousMeals: false, favorites: false });
  });

  it('accepts a Set, which is how both clients hold the selection', () => {
    expect(toRecipeQueryFilters(new Set(['Vegan', RECIPE_FILTER_FAVORITES]))).toEqual({
      tags: ['Vegan'],
      previousMeals: false,
      favorites: true,
    });
  });
});
