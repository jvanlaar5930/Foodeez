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
  darkMode?: boolean;
  unitSystem?: UnitSystem;
}

export interface GenerateMealPlanRequest {
  userId: string;
  startDate: string;
  endDate: string;
  name?: string;
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

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  role: ChatRole;
  content: string;
  /** Empty for most turns; non-empty when the reply proposed meals. */
  suggestions: PlannedMealDto[];
  suggestionsAcceptedAt?: string;
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
