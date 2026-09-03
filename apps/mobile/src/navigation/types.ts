import type { NavigatorScreenParams } from '@react-navigation/native';
import type { MealLogDto, MealType, QuickAddItemDto } from '@/types';

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
  MealLog: NavigatorScreenParams<MealLogStackParamList> | undefined;
  MealPlan: undefined;
  Grocery: undefined;
  Advice: undefined;
  Recipes: NavigatorScreenParams<RecipesStackParamList> | undefined;
  Profile: NavigatorScreenParams<ProfileStackParamList> | undefined;
};

export type ProfileStackParamList = {
  ProfileHome: undefined;
  EditProfile: undefined;
};

export type MealLogStackParamList = {
  MealLogHome: undefined;
  AddMeal: {
    mealType?: MealType;
    prefilledItems?: Array<{ name: string; servingSize: number; servingUnit: string }>;
    /** Items handed over by the photo scanner, already matched and ready to log. */
    parsedItems?: QuickAddItemDto[];
    mealLog?: MealLogDto;
  };
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
