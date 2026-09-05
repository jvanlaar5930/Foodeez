import { describe, expect, it, vi } from 'vitest';
import { caloriesOf, servingsOf, stepFor, useMealItems } from './useMealItems';
import type { FoodItem } from '@foodeez/shared';

/**
 * The amount maths lived inside AddMealModal, which computed the same servings ratio in three
 * separate places and reduced the whole list once per macro. What is pinned here is the part
 * that decides what gets stored: "150" against a food labelled per 100 g means one and a half
 * servings, and a food with no serving size recorded - hand-entered ones arrive like this -
 * must not divide by zero.
 */
function food(overrides: Partial<FoodItem> = {}): FoodItem {
  return {
    id: overrides.id ?? 'food-1',
    name: 'Rolled oats',
    brand: null,
    servingSize: 100,
    servingUnit: 'g',
    category: null,
    nutritionalInfo: {
      calories: 380,
      protein: 13,
      carbohydrates: 67,
      fat: 7,
      fiber: 10,
      sugar: 1,
      sodium: 5,
    },
    ...overrides,
  } as FoodItem;
}

describe('stepFor', () => {
  it.each(['g', 'gram', 'GRAMS', 'ml', 'Milliliters'])(
    'steps %s by 25, since half a gram is meaningless',
    (unit) => {
      expect(stepFor(food({ servingUnit: unit }))).toBe(25);
    },
  );

  it.each(['oz', 'serving', 'slice', 'cup'])('steps %s by half a unit', (unit) => {
    expect(stepFor(food({ servingUnit: unit }))).toBe(0.5);
  });
});

describe('servingsOf', () => {
  it('reads an amount as a fraction of the serving size', () => {
    expect(servingsOf({ item: food(), amount: 150 })).toBe(1.5);
  });

  it('treats a food with no serving size as labelled per unit', () => {
    // Dividing by zero would make every figure Infinity.
    expect(servingsOf({ item: food({ servingSize: 0 }), amount: 3 })).toBe(3);
  });

  it('scales calories with the amount', () => {
    expect(caloriesOf({ item: food(), amount: 50 })).toBe(190);
  });
});

describe('useMealItems', () => {
  it('adds a food at one serving', () => {
    const meal = useMealItems();

    meal.add(food());

    expect(meal.items.value).toHaveLength(1);
    expect(meal.items.value[0].amount).toBe(100);
  });

  it('adds a food with no serving size at one unit', () => {
    const meal = useMealItems();

    meal.add(food({ servingSize: 0 }));

    expect(meal.items.value[0].amount).toBe(1);
  });

  it('the same food twice is one line, not two helpings', () => {
    const meal = useMealItems();

    meal.add(food());
    meal.add(food(), { amount: 250, source: 'Estimated' });

    expect(meal.items.value).toHaveLength(1);
    expect(meal.items.value[0].amount).toBe(250);
    expect(meal.items.value[0].source).toBe('Estimated');
  });

  it('never lets an amount fall below one step', () => {
    const meal = useMealItems();
    meal.add(food());

    meal.adjust(meal.items.value[0], -1000);

    expect(meal.items.value[0].amount).toBe(25);
  });

  it('half-unit steps do not drift into floating-point noise', () => {
    const meal = useMealItems();
    meal.add(food({ servingUnit: 'serving', servingSize: 1 }));
    const entry = meal.items.value[0];

    for (let i = 0; i < 6; i += 1) {
      meal.adjust(entry, 0.1);
    }

    expect(entry.amount).toBe(1.6);
  });

  it('ignores an amount that is not a positive number', () => {
    const meal = useMealItems();
    meal.add(food());
    const entry = meal.items.value[0];

    meal.setAmount(entry, Number.NaN);
    meal.setAmount(entry, -5);
    meal.setAmount(entry, 0);

    expect(entry.amount).toBe(100);
  });

  it('totals every macro in one pass, scaled by amount', () => {
    const meal = useMealItems();
    meal.add(food());
    meal.setAmount(meal.items.value[0], 150);

    expect(meal.totals.value.map((macro) => macro.value)).toEqual([570, 20, 101, 11]);
  });

  it('totals nothing for an empty meal', () => {
    expect(useMealItems().totals.value.map((macro) => macro.value)).toEqual([0, 0, 0, 0]);
  });

  it('tells the caller about every edit, so a stale score can be retired', () => {
    // A score is only about the meal it was run on; the dialog drops it on any change.
    const onChange = vi.fn();
    const meal = useMealItems(onChange);

    meal.add(food());
    meal.adjust(meal.items.value[0], 25);
    meal.setAmount(meal.items.value[0], 200);
    meal.remove(0);

    expect(onChange).toHaveBeenCalledTimes(4);
  });

  it('seeding the list is not an edit', () => {
    // Opening the dialog on a saved meal must not retire the score stored on it.
    const onChange = vi.fn();
    const meal = useMealItems(onChange);

    meal.reset([{ item: food(), amount: 100 }]);

    expect(meal.items.value).toHaveLength(1);
    expect(onChange).not.toHaveBeenCalled();
  });
});
