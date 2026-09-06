/**
 * The app's types, which are the shared contract with the API.
 *
 * This file used to hold its own copy of all of them - 516 lines of enums, DTOs and constants
 * duplicating `@foodeez/shared`, which apps/web has always used. They drifted, as duplicated
 * definitions do: the enums here serialised as snake_case against an API that expects the C#
 * member names, so most profile saves were rejected outright, and the macro split disagreed
 * with both the server and the web app.
 *
 * Everything now comes from the one definition. The file stays as a re-export so that the
 * forty `from '@/types'` imports across the app keep working, and because `Dto`-suffixed names
 * read naturally at the call sites that use them.
 */

export * from '@foodeez/shared';

import type {
  ChatConversation,
  ChatConversationDetail,
  ChatMessage,
  ChatReply,
  DayAnalysis,
  FoodItem,
  GroceryItem,
  GroceryList,
  GroceryListState,
  MealAnalysis,
  MealLog,
  MealLogItem,
  MealPlan,
  MealPlanDay,
  MealPlanEntry,
  MealPlanGenerationResult,
  MealPlanProgress,
  MealTemplate,
  MealTemplateItem,
  NutritionSummary,
  PlannedMeal,
  QuickAddItem,
  QuickAddResult,
  RecentMeal,
  Recipe,
  RecipeIngredient,
  SuggestedRecipe,
  SuggestedRecipeIngredient,
  User,
  UserProfile,
} from '@foodeez/shared';

/**
 * The `Dto` spellings this app uses. Aliases rather than declarations, so there is still only
 * one definition of each shape - renaming the call sites can happen separately, or not at all.
 */
export type UserDto = User;
export type UserProfileDto = UserProfile;
export type FoodItemDto = FoodItem;
export type MealLogDto = MealLog;
export type MealLogItemDto = MealLogItem;
export type MealAnalysisDto = MealAnalysis;
export type DayAnalysisDto = DayAnalysis;
export type NutritionSummaryDto = NutritionSummary;
export type MealPlanDto = MealPlan;
export type MealPlanEntryDto = MealPlanEntry;
export type MealPlanProgressDto = MealPlanProgress;
export type MealPlanDayDto = MealPlanDay;
export type MealPlanGenerationResultDto = MealPlanGenerationResult;
export type RecipeDto = Recipe;
export type RecipeIngredientDto = RecipeIngredient;
export type MealTemplateDto = MealTemplate;
export type MealTemplateItemDto = MealTemplateItem;
export type QuickAddItemDto = QuickAddItem;
export type QuickAddResultDto = QuickAddResult;
export type RecentMealDto = RecentMeal;
export type PlannedMealDto = PlannedMeal;
export type ChatConversationDto = ChatConversation;
export type ChatConversationDetailDto = ChatConversationDetail;
export type ChatMessageDto = ChatMessage;
export type ChatReplyDto = ChatReply;
export type SuggestedRecipeDto = SuggestedRecipe;
export type SuggestedRecipeIngredientDto = SuggestedRecipeIngredient;
export type GroceryItemDto = GroceryItem;
export type GroceryListDto = GroceryList;
export type GroceryListStateDto = GroceryListState;
