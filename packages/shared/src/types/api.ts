import { ActivityLevel, DietaryGoal, Gender, MealType, UnitSystem } from '../enums';
import { MealAnalysis } from './meal';
import { MealPlanEntry } from './mealPlan';
import { NutritionalInfo } from './nutrition';
import { User, UserProfile } from './user';

// Auth
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}
export interface LoginRequest {
  email: string;
  password: string;
}
export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

// Users
export interface UpdateProfileRequest {
  heightCm: number;
  weightKg: number;
  targetWeightKg?: number;
  age: number;
  gender: Gender;
  activityLevel: ActivityLevel;
  dietaryGoal: DietaryGoal;
  notes?: string;
  /** Foods the AI must never suggest. Replaces the stored list wholesale. */
  excludedFoods?: string[];
  darkMode?: boolean;
  unitSystem?: UnitSystem;
}

// Meal Logs
export interface LogMealItemRequest {
  foodItemId: string;
  quantity: number;
  unit: string;
}
export interface LogMealRequest {
  userId: string;
  logDate: string;
  mealType: MealType;
  notes?: string;
  items: LogMealItemRequest[];
  /** An analysis already generated for exactly these items, persisted with the meal. */
  analysis?: MealAnalysis;
}

// Meal Plans
export interface CreateMealPlanRequest {
  userId: string;
  name: string;
  startDate: string;
  endDate: string;
}
export interface GenerateMealPlanRequest {
  userId: string;
  startDate: string;
  endDate: string;
  preferenceTags?: string[];
  excludeIngredients?: string[];
  /**
   * Free text for this one generation - "more variety in the dinners", "reuse last week's
   * breakfasts". Steers the plan without becoming a saved preference. When it is given the
   * server also shows the model the previous period, so an instruction that refers back to
   * last week has a real week to work from.
   */
  guidance?: string;
}

/**
 * One meal in one calendar slot. `notes` is how a meal with no recipe behind it gets its
 * name - the same field AI-generated meals use - so a slot needs a recipe, a food item or
 * notes, and the API rejects a request carrying none of the three.
 */
export interface MealPlanEntryRequest {
  entryDate: string;
  mealType: MealType;
  recipeId?: string;
  foodItemId?: string;
  notes?: string;
  servings: number;
}

/**
 * Where a meal is being dragged to - the destination only.
 *
 * Sending a whole `MealPlanEntryRequest` would mean rebuilding the recipe, servings and notes
 * the drag is not touching, and getting any of that subtly wrong would rewrite the meal while
 * appearing only to move it.
 */
export interface MealPlanEntryMoveRequest {
  entryDate: string;
  mealType: MealType;
  /**
   * The plan the destination day belongs to, when it is not the one the meal is in now.
   *
   * Needed wherever a plan covers less than the range being moved across: the mobile day
   * screen creates a plan for the single day it was opened on, so any change of date there
   * leaves that plan and the move would otherwise be refused as out of range.
   */
  targetPlanId?: string;
}

/**
 * What a move changed. A drop onto an occupied slot swaps the two meals rather than replacing
 * the one that was already there, so both cells have to be redrawn - `swapped` is the meal
 * that has taken the dragged one's old place, and is absent when the destination was empty.
 */
export interface MealPlanEntryMoveResponse {
  entry: MealPlanEntry;
  swapped?: MealPlanEntry;
}

// AI
export interface DietaryRecommendationsDto {
  suggestions: string[];
  deficiencies: string[];
  tips: string[];
  overallScore: number;
}
export interface ParsedFoodDto {
  name: string;
  brand?: string;
  servingSize: number;
  servingUnit: string;
  nutritionalInfo: NutritionalInfo;
  confidence: number;
}

// API error
export interface ApiError {
  status: number;
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

// Re-export for convenience
export type { User, UserProfile };
