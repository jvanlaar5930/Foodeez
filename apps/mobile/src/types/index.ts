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

export enum UnitSystem {
  US = 'US',
  Metric = 'Metric',
}

/**
 * Values are the API's own enum member names. The server serialises MealType with
 * JsonStringEnumConverter and no naming policy, so it emits "MorningSnack", not
 * "morning_snack" - and a mismatch here silently empties every meal slot rather than
 * failing loudly.
 */
export enum MealType {
  Breakfast = 'Breakfast',
  MorningSnack = 'MorningSnack',
  Lunch = 'Lunch',
  AfternoonSnack = 'AfternoonSnack',
  Dinner = 'Dinner',
  EveningSnack = 'EveningSnack',
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
  darkMode: boolean;
  unitSystem: UnitSystem;
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

/** What the model made of one meal. Stored with the meal and reused until its items change. */
export interface MealAnalysisDto {
  /** 0-100, where 100 is a fully balanced meal. */
  score: number;
  completeness: string;
  /** What the meal is short of - macros, food groups, micronutrients. */
  missing: string[];
  suggestions: string[];
  /** When the model produced it; absent for one that has never been stored. */
  generatedAt?: string;
}

/** What the model made of a whole day's eating, against that day's targets. */
export interface DayAnalysisDto {
  score: number;
  status: string;
  gaps: string[];
  /** Concrete things to eat that would round the day out. */
  recommendations: string[];
  generatedAt?: string;
}

export interface MealLogDto {
  id: string;
  logDate: string;
  mealType: MealType;
  notes?: string;
  items: MealLogItemDto[];
  totalNutrition: NutritionalInfo;
  /** The stored analysis, or absent while none has been generated for this meal. */
  analysis?: MealAnalysisDto;
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
  userId: string;
  name: string;
  startDate: string;
  endDate: string;
  isAIGenerated: boolean;
  /** Entries grouped by 'yyyy-MM-dd'. The API has never sent a flat `entries` array. */
  entriesByDate: Record<string, MealPlanEntryDto[]>;
}

export interface RecipeIngredientDto {
  foodItemId?: string;
  foodItemName: string;
  quantity: number;
  unit: string;
  notes?: string;
}

export interface RecipeDto {
  id: string;
  name: string;
  description?: string;
  /** Newline-separated steps, each already prefixed with its number by the API. */
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  /** Comma-joined tag list. */
  tags?: string;
  imageUrl?: string;
  isAIGenerated: boolean;
  /** Where the recipe was published, shown when we have no method of our own. */
  sourceUrl?: string;
  sourceName?: string;
  /** False when the upstream source has no method for this recipe at all. */
  hasInstructions?: boolean;
  /** True when the method could not be fetched this time - worth retrying, unlike the above. */
  detailUnavailable?: boolean;
  createdByUserId?: string;
  ingredients: RecipeIngredientDto[];
  nutritionalInfoPerServing: NutritionalInfo;
}

export interface DietaryRecommendationsDto {
  suggestions: string[];
  deficiencies: string[];
  tips: string[];
  overallScore: number;
}

/** Where a quick-added line's numbers came from. Shown per item so nothing is taken on trust. */
export type QuickAddSource =
  /** Straight out of a saved meal the user built and approved earlier. */
  | 'Saved'
  /** Matched to a food already in the database, whose nutrition is used as-is. */
  | 'Matched'
  /** Nothing matched, so these figures are the model's estimate and want a glance. */
  | 'Estimated';

export interface QuickAddItemDto {
  foodItem: FoodItemDto;
  quantity: number;
  unit: string;
  source: QuickAddSource;
  /** 0-1, how sure the model was. 1 for anything it did not have to guess. */
  confidence: number;
}

/**
 * What quick add worked out, for the user to look over. Nothing here has been logged - the
 * items go into the add-meal screen to be corrected, removed or added to before saving.
 */
export interface QuickAddResultDto {
  items: QuickAddItemDto[];
  mealTemplateId?: string;
  mealTemplateName?: string;
  /** The meal type to pre-select, when a saved meal said which one it is. */
  mealType?: MealType;
  /** What was assumed, or why nothing came back. */
  note?: string;
  /** True when a saved meal answered this, so no AI was involved and the items are exact. */
  fromSavedMeal: boolean;
}

export interface MealTemplateItemDto {
  foodItem: FoodItemDto;
  quantity: number;
  unit: string;
  nutritionalInfo: NutritionalInfo;
}

/** A meal saved under a name so it can be logged again in one tap. */
export interface MealTemplateDto {
  id: string;
  name: string;
  mealType?: MealType;
  items: MealTemplateItemDto[];
  totalNutrition: NutritionalInfo;
  timesUsed: number;
  lastUsedAt?: string;
}

/** A meal the user logged before, offered up for logging again as it stands. */
export interface RecentMealDto {
  mealLogId: string;
  logDate: string;
  mealType: MealType;
  /** The meal read as a line of text - "Rye bread, Turkey breast, Mayonnaise". */
  summary: string;
  items: MealTemplateItemDto[];
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
  darkMode?: boolean;
  unitSystem?: UnitSystem;
}

export interface GenerateMealPlanRequest {
  userId: string;
  startDate: string;
  endDate: string;
  name?: string;
  /**
   * Free text for this one generation - "more variety in the dinners", "reuse last week's
   * breakfasts". Steers the plan without becoming a saved preference. When it is given the
   * server also shows the model the previous period, so an instruction that refers back to
   * last week has a real week to work from.
   */
  guidance?: string;
}

// -- Advice chat --------------------------------------------------------------

/** Serialised by the API as the C# member name, so these values must match exactly. */
export enum ChatRole {
  User = 'User',
  Assistant = 'Assistant',
}

/** A meal the assistant offered to put on the calendar. */
export interface PlannedMealDto {
  date: string;
  mealType: MealType;
  name: string;
  description?: string;
  servings: number;
}

/** One line of a recipe the assistant wrote out. */
export interface SuggestedRecipeIngredientDto {
  name: string;
  quantity: number;
  unit: string;
  notes?: string;
}

/**
 * A recipe the assistant wrote out in full and offered to keep. It lives on the message
 * until the reader saves it, at which point it becomes a real entry in the recipe library.
 */
export interface SuggestedRecipeDto {
  name: string;
  description?: string;
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  tags?: string;
  ingredients: SuggestedRecipeIngredientDto[];
  /** Per serving, and the model's estimate rather than a measurement. */
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
  fiber: number;
  sugar: number;
  sodium: number;
}

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  role: ChatRole;
  content: string;
  /** Empty for most turns; non-empty when the reply proposed meals. */
  suggestions: PlannedMealDto[];
  suggestionsAcceptedAt?: string;
  /** Empty for most turns; non-empty when the reply set out how to cook something. */
  recipes: SuggestedRecipeDto[];
  /** Set once the recipes have been added to the library. */
  recipesSavedAt?: string;
  createdAt: string;
}

export interface ChatConversationDto {
  id: string;
  title: string;
  lastMessageAt: string;
  createdAt: string;
  messageCount: number;
  /** The opening of the last reply, so the list reads like an inbox. */
  preview?: string;
}

export interface ChatConversationDetailDto {
  id: string;
  title: string;
  lastMessageAt: string;
  createdAt: string;
  messages: ChatMessageDto[];
}

export interface SendChatMessageRequest {
  /** Omitted to start a new thread; the reply carries the id it was given. */
  conversationId?: string;
  message: string;
}

export interface ChatReplyDto {
  conversationId: string;
  title: string;
  message: ChatMessageDto;
}

/**
 * The stand-in picture for a recipe that came out of the advice tab.
 *
 * A recipe the assistant wrote has no photograph, and inventing one would misrepresent the
 * dish - so the thumbnail is the advice tab's own icon and label. This is a marker rather
 * than a fetchable URL: the screen draws the icon and text itself, which stays sharp at any
 * size and costs no request. Must match AiRecipeImage.Marker on the API.
 */
export const AI_RECIPE_IMAGE = 'foodeez://ask-ai';

/** True when a recipe's image is the advice-tab marker rather than a real picture. */
export function isAiRecipeImage(imageUrl?: string | null): boolean {
  return imageUrl?.toLowerCase() === AI_RECIPE_IMAGE;
}

// -- Grocery list -------------------------------------------------------------

/** The aisles a list is grouped by. The API normalises to exactly these. */
export const GROCERY_CATEGORIES = [
  'Produce',
  'Meat & Seafood',
  'Dairy & Eggs',
  'Bakery',
  'Pantry',
  'Frozen',
  'Drinks',
  'Other',
] as const;

export interface GroceryItemDto {
  id: string;
  name: string;
  /** How much to buy, written as it would be on a paper list: "500 g", "2 bunches". */
  quantity: string;
  category: string;
  /** Which planned meals wanted it. */
  source?: string;
  isChecked: boolean;
  /** Added or edited by hand, and so kept through a rebuild. */
  isCustom: boolean;
  sortOrder: number;
}

export interface GroceryListDto {
  id: string;
  startDate: string;
  endDate: string;
  generatedAt: string;
  /** True when the meal plan has changed since this list was compiled. */
  isStale: boolean;
  plannedMealCount: number;
  items: GroceryItemDto[];
}

export interface GroceryListStateDto {
  list: GroceryListDto | null;
  plannedMealCount: number;
}

export interface GenerateGroceryListRequest {
  startDate: string;
  endDate: string;
  /** Rebuild even when the stored list still matches the plan. Costs an AI call. */
  refresh?: boolean;
}

export interface GroceryItemRequest {
  name: string;
  quantity: string;
  category: string;
  isChecked?: boolean;
}
