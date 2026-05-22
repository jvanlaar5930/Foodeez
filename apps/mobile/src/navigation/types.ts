import type { NavigatorScreenParams } from '@react-navigation/native';
import { MealType } from '@/types';

export type RootStackParamList = {
  Auth: undefined;
  Main: undefined;
};

export type AuthStackParamList = {
  Welcome: undefined;
  Login: undefined;
  Register: undefined;
  ProfileSetup: undefined;
};

export type MainTabParamList = {
  Dashboard: undefined;
  MealLog: undefined;
  MealPlan: undefined;
  Recipes: NavigatorScreenParams<RecipesStackParamList> | undefined;
  Profile: undefined;
};

export type MealLogStackParamList = {
  MealLogHome: undefined;
  AddMeal: { mealType?: MealType; prefilledItems?: Array<{ name: string; servingSize: number; servingUnit: string }> };
  FoodScan: undefined;
};

export type MealPlanStackParamList = {
  MealPlanHome: undefined;
  CalendarDay: { date: string };
};

export type RecipesStackParamList = {
  RecipesList: { initialSearch?: string } | undefined;
  RecipeDetail: { recipeId: string };
};
