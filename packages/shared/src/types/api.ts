import { ActivityLevel, DietaryGoal, Gender, MealType } from '../enums';
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
