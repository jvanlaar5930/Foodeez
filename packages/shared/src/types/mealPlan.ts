import { MealType } from '../enums';

export interface MealPlanEntry {
  id: string;
  mealPlanId: string;
  entryDate: string;
  mealType: MealType;
  recipeId?: string;
  recipeName?: string;
  /**
   * True when the recipe in this slot is the enhanced version of another one. The two
   * versions share a name, so without this the calendar cannot say which one is being cooked.
   */
  recipeIsEnhanced?: boolean;
  foodItemId?: string;
  foodItemName?: string;
  notes?: string;
  servings: number;
}

export interface MealPlan {
  id: string;
  userId: string;
  name: string;
  startDate: string;
  endDate: string;
  isAIGenerated: boolean;
  /** Entries grouped by 'yyyy-MM-dd'. The API has never sent a flat `entries` array. */
  entriesByDate: Record<string, MealPlanEntry[]>;
  createdAt: string;
}

/**
 * How far along a generation is, sent once per day as the week is written.
 *
 * A plan is generated a day at a time rather than in one call, so there is real progress to
 * report - and, when something goes wrong, a date to name rather than a week that vanished.
 */
export interface MealPlanProgress {
  date: string;
  /** 1-based, so it reads as "day 3 of 7" without arithmetic. */
  dayNumber: number;
  totalDays: number;
  /** `kept` means every slot that day was already filled in and was left alone. */
  status: 'planning' | 'saved' | 'kept' | 'failed';
  message?: string;
}

/** One day's entries, delivered as soon as that day is saved rather than at the end. */
export interface MealPlanDay {
  date: string;
  planId: string;
  entries: MealPlanEntry[];
}

/** How a whole generation turned out, including the days that did not make it. */
export interface MealPlanGenerationResult {
  plan?: MealPlan;
  /** Dates nothing usable came back for, as 'yyyy-MM-dd'. */
  failedDates: string[];
  /** Dates left untouched because they were already planned, as 'yyyy-MM-dd'. */
  keptDates: string[];
  /** True when the run gave up early - repeated failures mean the provider is down. */
  stoppedEarly: boolean;
  /** A ready-to-show sentence summarising the above. */
  message?: string;
}
