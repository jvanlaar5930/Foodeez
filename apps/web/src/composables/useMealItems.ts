import { computed, ref, type Ref } from 'vue';
import type { FoodItem, QuickAddSource } from '@foodeez/shared';

/** One line in the meal being built: a food, and how much of it. */
export interface MealItem {
  item: FoodItem;
  /** In the food's own serving unit - 150 for a food labelled per 100 g. */
  amount: number;
  /** Where this line came from, when quick add put it here. Absent for a searched-for food. */
  source?: QuickAddSource;
}

/** One macro, ready to render. */
export interface MacroTotal {
  label: string;
  value: number;
  unit: string;
  color: string;
}

/**
 * How much the +/- buttons move by, in the food's own unit.
 *
 * Grams and millilitres come in quantities where a half is meaningless, so they step by 25.
 * Everything else is counted in servings, where a half is the useful increment.
 */
export function stepFor(item: FoodItem): number {
  switch (item.servingUnit.toLowerCase()) {
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
 * How many servings the amount comes to, which is what the label figures scale by.
 *
 * A food with no serving size recorded - hand-entered ones arrive like this - is read as
 * being labelled per unit, so the amount is the multiplier directly. This mirrors
 * `FoodItem.NutritionFor` on the server, which is what will be stored.
 */
export function servingsOf(entry: MealItem): number {
  const size = entry.item.servingSize;

  return !size || size <= 0 ? entry.amount : entry.amount / size;
}

/** What one line of the meal comes to, in calories. */
export function caloriesOf(entry: MealItem): number {
  return entry.item.nutritionalInfo.calories * servingsOf(entry);
}

export interface MealItems {
  items: Ref<MealItem[]>;
  totals: Ref<MacroTotal[]>;

  /**
   * Adds a food, or updates the line already there for it. `amount` is quick add's - it knows
   * how much was described; a food picked from search starts at one serving.
   */
  add(item: FoodItem, options?: { amount?: number; source?: QuickAddSource }): void;
  remove(index: number): void;
  /** Moves one line's amount by a step, never below one step. */
  adjust(entry: MealItem, delta: number): void;
  setAmount(entry: MealItem, amount: number): void;
  /** Replaces the whole list - opening the dialog on an existing meal. */
  reset(next?: MealItem[]): void;
}

/**
 * The list of foods being logged, and what it adds up to.
 *
 * The amount maths lived in AddMealModal, which computed the same servings ratio in three
 * separate places and reduced the whole list once per macro. Pulling it out is what lets the
 * row, the totals panel and the analysis request all agree about what "150 g" means.
 *
 * @param onChange Called after any edit. The dialog uses it to retire a stale AI score:
 * a analysis of a meal is only about the meal it was run on.
 */
export function useMealItems(onChange?: () => void): MealItems {
  const items = ref<MealItem[]>([]);

  function changed(): void {
    onChange?.();
  }

  function add(item: FoodItem, { amount, source }: { amount?: number; source?: QuickAddSource } = {}): void {
    // The same food twice in one meal is a second tap on the same button, not two helpings.
    const existing = items.value.find((entry) => entry.item.id === item.id);
    if (existing) {
      if (amount !== undefined) {
        existing.amount = amount;
      }
      existing.source = source ?? existing.source;
      changed();
      return;
    }

    // One serving by default, which is what someone picking a food out of search means.
    const defaultAmount = item.servingSize > 0 ? item.servingSize : 1;
    items.value.push({ item, amount: amount ?? defaultAmount, source });
    changed();
  }

  function remove(index: number): void {
    items.value.splice(index, 1);
    changed();
  }

  function adjust(entry: MealItem, delta: number): void {
    const step = stepFor(entry.item);
    // Rounded to two places: repeated 0.5 steps on a float otherwise drift into 1.4999999.
    const next = Math.round((entry.amount + delta) * 100) / 100;
    entry.amount = Math.max(step, next);
    changed();
  }

  function setAmount(entry: MealItem, amount: number): void {
    if (!Number.isNaN(amount) && amount > 0) {
      entry.amount = amount;
      changed();
    }
  }

  function reset(next: MealItem[] = []): void {
    items.value = next;
  }

  /** One pass over the list rather than one per macro. */
  const totals = computed<MacroTotal[]>(() => {
    const sum = { calories: 0, protein: 0, carbs: 0, fat: 0 };

    for (const entry of items.value) {
      const servings = servingsOf(entry);
      const nutrition = entry.item.nutritionalInfo;
      sum.calories += nutrition.calories * servings;
      sum.protein += nutrition.protein * servings;
      sum.carbs += nutrition.carbohydrates * servings;
      sum.fat += nutrition.fat * servings;
    }

    return [
      { label: 'Calories', value: Math.round(sum.calories), unit: 'kcal', color: 'text-green-600 dark:text-green-400' },
      { label: 'Protein', value: Math.round(sum.protein), unit: 'g', color: 'text-blue-600 dark:text-blue-400' },
      { label: 'Carbs', value: Math.round(sum.carbs), unit: 'g', color: 'text-orange-500 dark:text-orange-400' },
      { label: 'Fat', value: Math.round(sum.fat), unit: 'g', color: 'text-amber-500' },
    ];
  });

  return { items, totals, add, remove, adjust, setAmount, reset };
}
