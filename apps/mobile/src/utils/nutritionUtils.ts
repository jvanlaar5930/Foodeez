import { ActivityLevel, DietaryGoal, Gender } from '@/types';

/**
 * Nutrition targets, mirroring `UserProfile.CalculateAndSetTargets()` in the .NET domain.
 *
 * The server recalculates and stores these on every profile save, so what these produce is
 * only a preview of what it is about to store - they have to agree with it exactly, or the
 * setup screen shows one set of numbers and the profile then displays another.
 *
 * These duplicate `@foodeez/shared`'s nutrition utils, which this app cannot import yet.
 * Delete this half of the file once it can, and keep the two in step until then.
 */

const ACTIVITY_MULTIPLIERS: Record<ActivityLevel, number> = {
  [ActivityLevel.Sedentary]: 1.2,
  [ActivityLevel.LightlyActive]: 1.375,
  [ActivityLevel.ModeratelyActive]: 1.55,
  [ActivityLevel.VeryActive]: 1.725,
  [ActivityLevel.ExtraActive]: 1.9,
};

const GOAL_CALORIE_ADJUSTMENTS: Record<DietaryGoal, number> = {
  [DietaryGoal.WeightLoss]: -500,
  [DietaryGoal.WeightMaintenance]: 0,
  [DietaryGoal.WeightGain]: 300,
  [DietaryGoal.MuscleGain]: 300,
  [DietaryGoal.GeneralHealth]: 0,
};

const MIN_DAILY_CALORIES = 1200;

/** Carbs and fat split what is left after protein; the missing 10% is deliberate slack. */
const REMAINING_SPLIT = { carbs: 0.55, fat: 0.35 };

function round(value: number, places = 0): number {
  const factor = 10 ** places;
  return Math.round(value * factor) / factor;
}

/**
 * Calculate Basal Metabolic Rate using the Mifflin-St Jeor equation.
 * weight in kg, height in cm, age in years. Unrounded - the server multiplies the raw value.
 */
export function calculateBMR(
  weight: number,
  height: number,
  age: number,
  gender: Gender,
): number {
  const base = 10 * weight + 6.25 * height - 5 * age;
  // Female, Other and PreferNotToSay all use the female formula, as the server does.
  return gender === Gender.Male ? base + 5 : base - 161;
}

/** Calculate Total Daily Energy Expenditure. Unrounded. */
export function calculateTDEE(bmr: number, activityLevel: ActivityLevel): number {
  return bmr * (ACTIVITY_MULTIPLIERS[activityLevel] ?? 1.2);
}

/** Apply the goal adjustment to TDEE and enforce the safety floor. Unrounded. */
export function calculateCalorieTarget(tdee: number, goal: DietaryGoal): number {
  return Math.max(MIN_DAILY_CALORIES, tdee + (GOAL_CALORIE_ADJUSTMENTS[goal] ?? 0));
}

/**
 * Macro gram targets. Protein is set from body weight rather than as a share of calories,
 * then carbs and fat split what is left - which is why this needs the weight.
 */
export function calculateMacroTargets(
  calories: number,
  goal: DietaryGoal,
  weightKg: number,
): { protein: number; carbs: number; fat: number } {
  // The server rounds protein before deriving the remainder from it; same order here.
  const protein = round(weightKg * (goal === DietaryGoal.MuscleGain ? 1.2 : 0.8), 1);
  const remaining = calories - protein * 4;

  return {
    protein,
    carbs: round((remaining * REMAINING_SPLIT.carbs) / 4, 1),
    fat: round((remaining * REMAINING_SPLIT.fat) / 9, 1),
  };
}

/** The whole calculation end to end, in the server's order. Prefer this over the steps. */
export function calculateTargets(input: {
  weightKg: number;
  heightCm: number;
  age: number;
  gender: Gender;
  activityLevel: ActivityLevel;
  dietaryGoal: DietaryGoal;
}): { calories: number; protein: number; carbs: number; fat: number } {
  const bmr = calculateBMR(input.weightKg, input.heightCm, input.age, input.gender);
  const calories = calculateCalorieTarget(
    calculateTDEE(bmr, input.activityLevel),
    input.dietaryGoal,
  );

  return {
    calories: round(calories),
    ...calculateMacroTargets(calories, input.dietaryGoal, input.weightKg),
  };
}

/**
 * Format calories with thousands separator and unit.
 */
export function formatCalories(calories: number): string {
  return `${Math.round(calories).toLocaleString()} kcal`;
}

/**
 * Format macro grams value.
 */
export function formatMacro(grams: number): string {
  return `${Math.round(grams)}g`;
}

/**
 * Calculate BMI from weight (kg) and height (cm).
 */
export function calculateBMI(weight: number, heightCm: number): number {
  const heightM = heightCm / 100;
  return Math.round((weight / (heightM * heightM)) * 10) / 10;
}

/**
 * Clamp a percentage value between 0 and 100.
 */
export function clampPercentage(value: number): number {
  return Math.min(100, Math.max(0, value));
}
