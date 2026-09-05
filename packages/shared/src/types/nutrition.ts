import { MealType } from '../enums';

export interface NutritionalInfo {
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
  fiber: number;
  sugar: number;
  sodium: number;
}

export interface NutritionSummary {
  date: string;
  totalCalories: number;
  totalProtein: number;
  totalCarbs: number;
  totalFat: number;
  totalFiber: number;
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFat: number;
  caloriesPercentage: number;
  proteinPercentage: number;
  carbsPercentage: number;
  fatPercentage: number;
}

/**
 * A range of days added up for the reporting screens, as `GET /meal-logs/report` returns it.
 * Everything here is computed server-side so a chart costs one request rather than a download
 * of every meal in the range.
 */
export interface NutritionReport {
  startDate: string;
  endDate: string;
  /** One entry per day in the range, in order, including days with nothing logged. */
  days: NutritionReportDay[];
  /** Averaged over logged days only - an untouched day is a gap, not a day of eating nothing. */
  averages: NutritionTotals;
  totals: NutritionTotals;
  daysInRange: number;
  daysLogged: number;
  /** Logged days landing within a tenth of the calorie target, either side. */
  daysOnTarget: number;
  totalMeals: number;
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFat: number;
  byMealType: MealTypeTotal[];
  /** The foods logged most often in the range, most frequent first. */
  topFoods: LoggedFoodTotal[];
}

export interface NutritionReportDay {
  date: string;
  /** False for a day with no meals at all, which the charts draw as a gap. */
  hasLogs: boolean;
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
  mealCount: number;
}

export interface NutritionTotals {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  fiber: number;
}

export interface MealTypeTotal {
  mealType: MealType;
  calories: number;
  mealCount: number;
}

export interface LoggedFoodTotal {
  name: string;
  timesLogged: number;
  calories: number;
}
