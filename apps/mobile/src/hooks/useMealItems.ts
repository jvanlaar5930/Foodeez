import { useCallback, useMemo, useState } from 'react';
import type { FoodItemDto, QuickAddSource } from '@/types';

/** One line in the meal being built: a food, and how much of it. */
export interface MealItem {
  foodItem: FoodItemDto;
  /** In the food's own serving unit - 150 for a food labelled per 100 g. */
  quantity: number;
  /** Where this line came from, when quick add or a scan put it here. */
  source?: QuickAddSource;
}

export interface MealTotals {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
}

/**
 * How much the +/- buttons move by, in the food's own unit.
 *
 * Grams and millilitres come in quantities where a half is meaningless, so they step by 25.
 * Everything else is counted in servings, where a half is the useful increment. This is the
 * web client's rule; mobile used to derive the step from the serving size instead, which made
 * the step as big as whatever a label happened to say. A loaf scanned as 10 slices per
 * container got a step of 5, and since the step is also the floor, the only amounts reachable
 * were 5, 10, 15 - two slices could not be logged at all.
 */
export function stepFor(item: FoodItemDto): number {
  switch (item.servingUnit?.toLowerCase()) {
    case 'g':
    case 'gram':
    case 'grams':
    case 'ml':
    case 'milliliter':
    case 'milliliters':
      return 25;
    default:
      return 0.5;
  }
}

/**
 * How many servings the quantity comes to, which is what the label figures scale by.
 *
 * A food with no serving size recorded - hand-entered ones arrive like this - is read as
 * labelled per unit, so the quantity is the multiplier directly. This mirrors
 * `FoodItem.NutritionFor` on the server, which is what will actually be stored, and the web
 * client's `servingsOf`.
 */
export function servingsOf(entry: MealItem): number {
  const size = entry.foodItem.servingSize;

  return size > 0 ? entry.quantity / size : entry.quantity;
}

/** One line's nutrition, scaled to the quantity. Used for the meal totals and the AI request. */
export function nutritionOf(entry: MealItem): MealTotals & { fiber: number } {
  const servings = servingsOf(entry);
  const n = entry.foodItem.nutritionalInfo;

  return {
    calories: n.calories * servings,
    protein: n.protein * servings,
    carbs: n.carbohydrates * servings,
    fat: n.fat * servings,
    fiber: (n.fiber ?? 0) * servings,
  };
}

export interface UseMealItems {
  items: MealItem[];
  totals: MealTotals;
  /** Whether the meal has been changed since it was seeded, so a stored score is stale. */
  edited: boolean;

  /** Adds a food, or removes it if it is already there - the search row is a toggle. */
  toggle: (foodItem: FoodItemDto) => void;
  /** Adds or replaces a line by food id. What quick add and the photo scanner hand over. */
  put: (entries: MealItem[]) => void;
  /** Moves one line by a step, never below one step. */
  adjust: (foodItemId: string, delta: number) => void;
  /**
   * Sets one line to an amount outright, which is the only way to reach one the steps do not
   * land on - two slices of a loaf whose label counts in tens. Anything that is not a positive
   * number is ignored, leaving the line as it was.
   */
  setQuantity: (foodItemId: string, quantity: number) => void;
  remove: (foodItemId: string) => void;
  has: (foodItemId: string) => boolean;
  /** Replaces the list without counting as an edit - opening the screen on a saved meal. */
  reset: (entries: MealItem[]) => void;
  /** Marks the meal as changed for something outside the list, like the meal type. */
  markEdited: () => void;
}

/**
 * The list of foods being logged, and what it adds up to.
 *
 * The screen computed the same servings ratio in three separate places - the totals, the AI
 * request, and each row's calorie figure - and each one wrote out the "no serving size
 * recorded" rule again. It is one function now, matching the server and the web client.
 */
export function useMealItems(initial: MealItem[] = []): UseMealItems {
  const [items, setItems] = useState<MealItem[]>(initial);
  const [edited, setEdited] = useState(false);

  const markEdited = useCallback(() => setEdited(true), []);

  const toggle = useCallback((foodItem: FoodItemDto) => {
    setEdited(true);
    setItems((prev) =>
      prev.some((entry) => entry.foodItem.id === foodItem.id)
        ? prev.filter((entry) => entry.foodItem.id !== foodItem.id)
        : [...prev, { foodItem, quantity: foodItem.servingSize || 1 }],
    );
  }, []);

  const put = useCallback((entries: MealItem[]) => {
    if (entries.length === 0) return;

    setEdited(true);
    setItems((prev) => {
      const next = [...prev];
      for (const entry of entries) {
        // The same food twice in one meal is a second tap, not two helpings.
        const at = next.findIndex((existing) => existing.foodItem.id === entry.foodItem.id);
        if (at >= 0) next[at] = entry;
        else next.push(entry);
      }
      return next;
    });
  }, []);

  const adjust = useCallback((foodItemId: string, delta: number) => {
    setEdited(true);
    setItems((prev) =>
      prev.map((entry) => {
        if (entry.foodItem.id !== foodItemId) return entry;

        const step = stepFor(entry.foodItem);
        // Rounded to two places: repeated fractional steps otherwise drift into 1.4999999.
        const next = Math.round((entry.quantity + delta) * 100) / 100;
        return { ...entry, quantity: Math.max(step, next) };
      }),
    );
  }, []);

  const setQuantity = useCallback((foodItemId: string, quantity: number) => {
    if (!Number.isFinite(quantity) || quantity <= 0) return;

    setEdited(true);
    setItems((prev) =>
      prev.map((entry) =>
        entry.foodItem.id === foodItemId ? { ...entry, quantity } : entry,
      ),
    );
  }, []);

  const remove = useCallback((foodItemId: string) => {
    setEdited(true);
    setItems((prev) => prev.filter((entry) => entry.foodItem.id !== foodItemId));
  }, []);

  const has = useCallback(
    (foodItemId: string) => items.some((entry) => entry.foodItem.id === foodItemId),
    [items],
  );

  const reset = useCallback((entries: MealItem[]) => {
    setItems(entries);
    setEdited(false);
  }, []);

  /** One pass over the list rather than one per macro. */
  const totals = useMemo(
    () =>
      items.reduce<MealTotals>(
        (sum, entry) => {
          const n = nutritionOf(entry);
          return {
            calories: sum.calories + n.calories,
            protein: sum.protein + n.protein,
            carbs: sum.carbs + n.carbs,
            fat: sum.fat + n.fat,
          };
        },
        { calories: 0, protein: 0, carbs: 0, fat: 0 },
      ),
    [items],
  );

  return { items, totals, edited, toggle, put, adjust, setQuantity, remove, has, reset, markEdited };
}
