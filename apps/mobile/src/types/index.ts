// Enums
export enum Gender {
  Male = 'male',
  Female = 'female',
  Other = 'other',
  PreferNotToSay = 'prefer_not_to_say',
}

export enum ActivityLevel {
  Sedentary = 'sedentary',
  LightlyActive = 'lightly_active',
  ModeratelyActive = 'moderately_active',
  VeryActive = 'very_active',
  ExtraActive = 'extra_active',
}

export enum DietaryGoal {
  WeightLoss = 'weight_loss',
  WeightMaintenance = 'weight_maintenance',
  WeightGain = 'weight_gain',
  MuscleGain = 'muscle_gain',
  GeneralHealth = 'general_health',
}

export enum MealType {
  Breakfast = 'breakfast',
  MorningSnack = 'morning_snack',
  Lunch = 'lunch',
  AfternoonSnack = 'afternoon_snack',
  Dinner = 'dinner',
  EveningSnack = 'evening_snack',
}

// Core types
export interface NutritionalInfo {
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
  fiber: number;
  sugar: number;
  sodium: number;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileCompleted: boolean;
}

export interface UserProfileDto {
  userId: string;
  heightCm: number;
  weightKg: number;
  targetWeightKg: number;
  age: number;
  gender: Gender;
  activityLevel: ActivityLevel;
  dietaryGoal: DietaryGoal;
  dailyCalorieTarget: number;
  dailyProteinTargetG: number;
  dailyCarbTargetG: number;
  dailyFatTargetG: number;
  notes?: string;
  profileCompleted: boolean;
}

export interface FoodItemDto {
  id: string;
  name: string;
  brand?: string;
  servingSize: number;
  servingUnit: string;
  category?: string;
  nutritionalInfo: NutritionalInfo;
}

export interface MealLogItemDto {
  id: string;
  foodItem: FoodItemDto;
  quantity: number;
  unit: string;
  nutritionalInfo: NutritionalInfo;
}

export interface MealLogDto {
  id: string;
  logDate: string;
  mealType: MealType;
  notes?: string;
  items: MealLogItemDto[];
  totalNutrition: NutritionalInfo;
}

export interface NutritionSummaryDto {
  date: string;
  totalCalories: number;
  totalProtein: number;
  totalCarbs: number;
  totalFat: number;
  targetCalories: number;
  targetProtein: number;
  targetCarbs: number;
  targetFat: number;
  caloriesPercentage: number;
  proteinPercentage: number;
  carbsPercentage: number;
  fatPercentage: number;
}

export interface MealPlanEntryDto {
  id: string;
  entryDate: string;
  mealType: MealType;
  recipeName?: string;
  foodItemName?: string;
  notes?: string;
  servings: number;
}

export interface MealPlanDto {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isAIGenerated: boolean;
  entries: MealPlanEntryDto[];
}

export interface RecipeIngredientDto {
  foodItemId: string;
  foodItemName: string;
  quantity: number;
  unit: string;
  notes?: string;
}

export interface RecipeDto {
  id: string;
  name: string;
  description?: string;
  instructions: string[];
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  tags: string[];
  imageUrl?: string;
  isAIGenerated: boolean;
  ingredients: RecipeIngredientDto[];
  nutritionalInfoPerServing: NutritionalInfo;
}

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

// Request types
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  user: UserDto;
  token: string;
}

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

export interface UpdateProfileRequest {
  heightCm?: number;
  weightKg?: number;
  targetWeightKg?: number;
  age?: number;
  gender?: Gender;
  activityLevel?: ActivityLevel;
  dietaryGoal?: DietaryGoal;
  notes?: string;
}

export interface GenerateMealPlanRequest {
  userId: string;
  startDate: string;
  endDate: string;
  name?: string;
}
