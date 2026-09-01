import { MealType } from '../enums';

export interface MealPlanEntry {
  id: string;
  mealPlanId: string;
  entryDate: string;
  mealType: MealType;
  recipeId?: string;
  recipeName?: string;
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
