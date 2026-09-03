import { MealType } from '../enums';
import { NutritionalInfo } from './nutrition';

export interface FoodItem {
  id: string;
  name: string;
  brand?: string;
  servingSize: number;
  servingUnit: string;
  category?: string;
  barcode?: string;
  isCustom: boolean;
  nutritionalInfo: NutritionalInfo;
}

export interface MealLogItem {
  id: string;
  mealLogId: string;
  foodItem: FoodItem;
  quantity: number;
  unit: string;
  nutritionalInfo: NutritionalInfo;
}

/** An AI verdict on a meal, stored with the meal log once generated. */
export interface MealAnalysis {
  score: number;
  completeness: string;
  missing: string[];
  suggestions: string[];
  /** When the model produced it; absent for an analysis that has never been stored. */
  generatedAt?: string;
}

/** An AI read on a whole day of eating, stored per user per day once generated. */
export interface DayAnalysis {
  score: number;
  /** A short assessment of where the day stands. */
  status: string;
  /** What the day is short of - macros, food groups, micronutrients. */
  gaps: string[];
  /** Concrete things to eat that would round the day out. */
  recommendations: string[];
  generatedAt?: string;
}

export interface MealLog {
  id: string;
  userId: string;
  logDate: string;
  mealType: MealType;
  notes?: string;
  items: MealLogItem[];
  totalNutrition: NutritionalInfo;
  analysis?: MealAnalysis;
  createdAt: string;
}

/** Where a quick-added line's numbers came from. Shown per item so nothing is taken on trust. */
export type QuickAddSource =
  /** Straight out of a saved meal the user built and approved earlier. */
  | 'Saved'
  /** Matched to a food already in the database, whose nutrition is used as-is. */
  | 'Matched'
  /** Nothing matched, so these figures are the model's estimate and want a glance. */
  | 'Estimated';

export interface QuickAddItem {
  foodItem: FoodItem;
  quantity: number;
  unit: string;
  source: QuickAddSource;
  /** 0-1, how sure the model was. 1 for anything it did not have to guess. */
  confidence: number;
}

/**
 * What quick add worked out, for the user to look over. Nothing here has been logged - the
 * items go into the meal dialog to be corrected, removed or added to before saving.
 */
export interface QuickAddResult {
  items: QuickAddItem[];
  mealTemplateId?: string;
  mealTemplateName?: string;
  /** The meal type to pre-select, when a saved meal said which one it is. */
  mealType?: MealType;
  /** What was assumed, or why nothing came back. */
  note?: string;
  /** True when a saved meal answered this, so no AI was involved and the items are exact. */
  fromSavedMeal: boolean;
}

export interface MealTemplateItem {
  foodItem: FoodItem;
  quantity: number;
  unit: string;
  nutritionalInfo: NutritionalInfo;
}

/** A meal saved under a name so it can be logged again in one tap. */
export interface MealTemplate {
  id: string;
  name: string;
  mealType?: MealType;
  items: MealTemplateItem[];
  totalNutrition: NutritionalInfo;
  timesUsed: number;
  lastUsedAt?: string;
}

/** A meal the user logged before, offered up for logging again as it stands. */
export interface RecentMeal {
  mealLogId: string;
  logDate: string;
  mealType: MealType;
  /** The meal read as a line of text - "Rye bread, Turkey breast, Mayonnaise". */
  summary: string;
  items: MealTemplateItem[];
  totalNutrition: NutritionalInfo;
  /** True when this exact set of items is already saved under a name. */
  isSaved: boolean;
}

export interface SaveMealTemplateRequest {
  userId: string;
  name: string;
  mealType?: MealType;
  items: { foodItemId: string; quantity: number; unit: string }[];
}
