import { act, renderHook } from '@testing-library/react-native';
import { nutritionOf, servingsOf, stepFor, useMealItems, type MealItem } from './useMealItems';
import type { FoodItemDto } from '@/types';

/**
 * AddMealScreen computed the same servings ratio in three separate places - the totals, the
 * AI request, and each row's calorie figure - and wrote out the "no serving size recorded"
 * rule again each time. That rule decides what gets stored, so it is what is pinned here.
 */
function food(overrides: Partial<FoodItemDto> = {}): FoodItemDto {
  return {
    id: 'food-1',
    name: 'Rolled oats',
    servingSize: 100,
    servingUnit: 'g',
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
  } as FoodItemDto;
}

describe('servingsOf', () => {
  it('reads a quantity as a fraction of the serving size', () => {
    expect(servingsOf({ foodItem: food(), quantity: 150 })).toBe(1.5);
  });

  it('treats a food with no serving size as labelled per unit', () => {
    // Dividing by zero would make every figure Infinity.
    expect(servingsOf({ foodItem: food({ servingSize: 0 }), quantity: 3 })).toBe(3);
  });
});

describe('stepFor', () => {
  it('is half a serving for a food that has one', () => {
    expect(stepFor(food({ servingSize: 30 }))).toBe(15);
  });

  it('never drops below half a unit, however small the serving', () => {
    expect(stepFor(food({ servingSize: 0.4 }))).toBe(0.5);
    expect(stepFor(food({ servingSize: 0 }))).toBe(0.5);
  });
});

describe('nutritionOf', () => {
  it('scales every figure, fibre included', () => {
    const n = nutritionOf({ foodItem: food(), quantity: 50 });

    expect(n.calories).toBe(190);
    expect(n.protein).toBe(6.5);
    expect(n.fiber).toBe(5);
  });

  it('reads a missing fibre figure as zero rather than NaN', () => {
    const item = food();
    // @ts-expect-error - deliberately modelling a row the API left fibre off
    delete item.nutritionalInfo.fiber;

    expect(nutritionOf({ foodItem: item, quantity: 100 }).fiber).toBe(0);
  });
});

describe('useMealItems', () => {
  it('adds a food at one serving', () => {
    const { result } = renderHook(() => useMealItems());

    act(() => result.current.toggle(food()));

    expect(result.current.items).toHaveLength(1);
    expect(result.current.items[0].quantity).toBe(100);
  });

  it('tapping the same search row again takes it back out', () => {
    const { result } = renderHook(() => useMealItems());

    act(() => result.current.toggle(food()));
    act(() => result.current.toggle(food()));

    expect(result.current.items).toHaveLength(0);
  });

  it('quick add replaces a line rather than adding a second helping', () => {
    const { result } = renderHook(() => useMealItems());
    act(() => result.current.toggle(food()));

    act(() => result.current.put([{ foodItem: food(), quantity: 250, source: 'Estimated' }]));

    expect(result.current.items).toHaveLength(1);
    expect(result.current.items[0].quantity).toBe(250);
    expect(result.current.items[0].source).toBe('Estimated');
  });

  it('never lets a quantity fall below one step', () => {
    const { result } = renderHook(() => useMealItems());
    act(() => result.current.toggle(food()));

    act(() => result.current.adjust('food-1', -1000));

    expect(result.current.items[0].quantity).toBe(50);
  });

  it('fractional steps do not drift into floating-point noise', () => {
    const { result } = renderHook(() => useMealItems());
    act(() => result.current.toggle(food({ servingSize: 1 })));

    for (let i = 0; i < 6; i += 1) {
      act(() => result.current.adjust('food-1', 0.1));
    }

    expect(result.current.items[0].quantity).toBe(1.6);
  });

  it('totals every macro in one pass', () => {
    const { result } = renderHook(() => useMealItems());
    act(() => result.current.toggle(food()));
    act(() => result.current.adjust('food-1', 50));

    expect(result.current.totals.calories).toBeCloseTo(570, 5);
    expect(result.current.totals.protein).toBeCloseTo(19.5, 5);
  });

  it('totals nothing for an empty meal', () => {
    const { result } = renderHook(() => useMealItems());

    expect(result.current.totals).toEqual({ calories: 0, protein: 0, carbs: 0, fat: 0 });
  });

  it('counts every edit, so a stale score can be retired', () => {
    const { result } = renderHook(() => useMealItems());
    expect(result.current.edited).toBe(false);

    act(() => result.current.toggle(food()));

    expect(result.current.edited).toBe(true);
  });

  it('seeding the list is not an edit', () => {
    // Opening the screen on a saved meal must not retire the score stored on it.
    const seeded: MealItem[] = [{ foodItem: food(), quantity: 100 }];
    const { result } = renderHook(() => useMealItems());

    act(() => result.current.reset(seeded));

    expect(result.current.items).toHaveLength(1);
    expect(result.current.edited).toBe(false);
  });

  it('an empty put changes nothing, so an effect can call it unconditionally', () => {
    const { result } = renderHook(() => useMealItems());

    act(() => result.current.put([]));

    expect(result.current.edited).toBe(false);
  });
});
