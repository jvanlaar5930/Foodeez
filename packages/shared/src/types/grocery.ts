/** The aisles a list is grouped by. The API normalises to exactly these. */
export const GROCERY_CATEGORIES = [
  'Produce',
  'Meat & Seafood',
  'Dairy & Eggs',
  'Bakery',
  'Pantry',
  'Frozen',
  'Drinks',
  'Other',
] as const;

export type GroceryCategory = (typeof GROCERY_CATEGORIES)[number];

export interface GroceryItem {
  id: string;
  name: string;
  /** How much to buy, written as it would be on a paper list: "500 g", "2 bunches". */
  quantity: string;
  category: string;
  /** Which planned meals wanted it. */
  source?: string;
  isChecked: boolean;
  /** Added or edited by hand, and so kept through a rebuild. */
  isCustom: boolean;
  sortOrder: number;
}

export interface GroceryList {
  id: string;
  startDate: string;
  endDate: string;
  generatedAt: string;
  /** True when the meal plan has changed since this list was compiled. */
  isStale: boolean;
  plannedMealCount: number;
  items: GroceryItem[];
}

/**
 * A range's state in one response: the stored list if there is one, and how many meals are
 * planned - the difference between "nothing planned yet" and "planned, not yet compiled".
 */
export interface GroceryListState {
  list: GroceryList | null;
  plannedMealCount: number;
}

export interface GenerateGroceryListRequest {
  startDate: string;
  endDate: string;
  /** Rebuild even when the stored list still matches the plan. Costs an AI call. */
  refresh?: boolean;
}

/** An item added or changed by hand. The same shape serves both. */
export interface GroceryItemRequest {
  name: string;
  quantity: string;
  category: string;
  isChecked?: boolean;
}
