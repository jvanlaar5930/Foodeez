import { MealType } from '../enums';

/**
 * Target-calculation constants.
 *
 * These mirror `UserProfile.CalculateAndSetTargets()` in the .NET domain layer, which is the
 * system of record: the server recomputes and stores targets on every profile save, so a
 * client that calculates them differently is only ever showing a preview of a number that is
 * about to be overwritten with a different one. Change these only alongside that method.
 */
export const ACTIVITY_LEVEL_MULTIPLIERS = {
  Sedentary: 1.2,
  LightlyActive: 1.375,
  ModeratelyActive: 1.55,
  VeryActive: 1.725,
  ExtraActive: 1.9,
} as const;

export const GOAL_CALORIE_ADJUSTMENTS = {
  WeightLoss: -500,
  WeightMaintenance: 0,
  WeightGain: 300,
  MuscleGain: 300,
  GeneralHealth: 0,
} as const;

/** Floor applied after the goal adjustment, so a deficit can never recommend a unsafe intake. */
export const MIN_DAILY_CALORIES = 1200;

/**
 * Protein is set from body weight rather than as a share of calories - the requirement tracks
 * lean mass, not intake, so a cut and a bulk at the same weight need the same protein.
 */
export const PROTEIN_G_PER_KG = {
  default: 0.8,
  MuscleGain: 1.2,
} as const;

/**
 * Carbs and fat split what is left after protein. These deliberately sum to 0.90, not 1.0 -
 * the remaining 10% is slack the server leaves unallocated rather than forcing every calorie
 * into a macro target.
 */
export const REMAINING_CALORIE_SPLIT = {
  carbs: 0.55,
  fat: 0.35,
} as const;

/** kcal per gram. */
export const CALORIES_PER_GRAM = {
  protein: 4,
  carbs: 4,
  fat: 9,
} as const;

export const DAILY_REFERENCE_VALUES = {
  calories: 2000,
  protein: 50,
  carbohydrates: 275,
  fat: 78,
  fiber: 28,
  sugar: 50,
  sodium: 2300,
} as const;

export const MEAL_TYPE_LABELS: Record<MealType, string> = {
  [MealType.Breakfast]: 'Breakfast',
  [MealType.MorningSnack]: 'Morning Snack',
  [MealType.Lunch]: 'Lunch',
  [MealType.AfternoonSnack]: 'Afternoon Snack',
  [MealType.Dinner]: 'Dinner',
  [MealType.EveningSnack]: 'Evening Snack',
};
