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
