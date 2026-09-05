import {
  ACTIVITY_LEVEL_MULTIPLIERS,
  CALORIES_PER_GRAM,
  GOAL_CALORIE_ADJUSTMENTS,
  MIN_DAILY_CALORIES,
  PROTEIN_G_PER_KG,
  REMAINING_CALORIE_SPLIT,
} from '../constants/nutrition';

/**
 * Nutrition targets, mirroring `UserProfile.CalculateAndSetTargets()` in the .NET domain.
 *
 * The server recalculates and stores these on every profile save, so these functions exist
 * only to preview what it is about to store. They must therefore agree with it exactly -
 * a client formula that "looks more correct" than the server's just shows the user a number
 * that changes the moment they press save.
 */

/** Round to `places` decimals, matching C#'s `Math.Round(value, places)` for these ranges. */
function round(value: number, places = 0): number {
  const factor = 10 ** places;
  return Math.round(value * factor) / factor;
}

/**
 * Calculate Basal Metabolic Rate using the Mifflin-St Jeor equation.
 * @param weightKg - weight in kilograms
 * @param heightCm - height in centimetres
 * @param age - age in years
 * @param gender - Gender enum value; every non-Male value uses the female formula, as the server does
 * @returns BMR in kcal/day, unrounded
 */
export function calculateBMR(
  weightKg: number,
  heightCm: number,
  age: number,
  gender: string,
): number {
  const base = 10 * weightKg + 6.25 * heightCm - 5 * age;
  return gender === 'Male' ? base + 5 : base - 161;
}

/**
 * Calculate Total Daily Energy Expenditure.
 * @param bmr - Basal Metabolic Rate in kcal/day
 * @param activityLevel - ActivityLevel enum value
 * @returns TDEE in kcal/day, unrounded - round only for display
 */
export function calculateTDEE(bmr: number, activityLevel: string): number {
  const multiplier =
    (ACTIVITY_LEVEL_MULTIPLIERS as Record<string, number>)[activityLevel] ?? 1.2;
  return bmr * multiplier;
}

/**
 * Apply the goal adjustment to TDEE and enforce the safety floor.
 * @returns the daily calorie target, unrounded
 */
export function calculateCalorieTarget(tdee: number, goal: string): number {
  const adjustment = (GOAL_CALORIE_ADJUSTMENTS as Record<string, number>)[goal] ?? 0;
  return Math.max(MIN_DAILY_CALORIES, tdee + adjustment);
}

/**
 * Calculate daily macro gram targets.
 *
 * Protein comes from body weight, then carbs and fat split what is left of the calorie
 * target - which is why this needs the weight and not just the calories.
 *
 * @param calories - the daily calorie target, as returned by {@link calculateCalorieTarget}
 * @param goal - DietaryGoal enum value
 * @param weightKg - body weight in kilograms
 */
export function calculateMacroTargets(
  calories: number,
  goal: string,
  weightKg: number,
): { proteinG: number; carbsG: number; fatG: number } {
  const perKg =
    (PROTEIN_G_PER_KG as Record<string, number>)[goal] ?? PROTEIN_G_PER_KG.default;

  // The server rounds protein before deriving the remainder from it, so rounding here in the
  // same order is what keeps the two in step.
  const proteinG = round(weightKg * perKg, 1);
  const remaining = calories - proteinG * CALORIES_PER_GRAM.protein;

  return {
    proteinG,
    carbsG: round((remaining * REMAINING_CALORIE_SPLIT.carbs) / CALORIES_PER_GRAM.carbs, 1),
    fatG: round((remaining * REMAINING_CALORIE_SPLIT.fat) / CALORIES_PER_GRAM.fat, 1),
  };
}

/**
 * The whole calculation end to end, in the server's order. Prefer this over calling the steps
 * individually - it is the one that is guaranteed to match what a profile save will store.
 */
export function calculateTargets(input: {
  weightKg: number;
  heightCm: number;
  age: number;
  gender: string;
  activityLevel: string;
  dietaryGoal: string;
}): { calories: number; proteinG: number; carbsG: number; fatG: number } {
  const bmr = calculateBMR(input.weightKg, input.heightCm, input.age, input.gender);
  const tdee = calculateTDEE(bmr, input.activityLevel);
  const calories = calculateCalorieTarget(tdee, input.dietaryGoal);

  return {
    calories: round(calories),
    ...calculateMacroTargets(calories, input.dietaryGoal, input.weightKg),
  };
}

/**
 * Format a nutrition value with its unit for display.
 * @returns formatted string like "42 g"
 */
export function formatNutritionValue(value: number, unit: string): string {
  const rounded = Number.isInteger(value) ? value : parseFloat(value.toFixed(1));
  return `${rounded} ${unit}`;
}

/** Calculate Body Mass Index from weight (kg) and height (cm). */
export function calculateBMI(weightKg: number, heightCm: number): number {
  const heightM = heightCm / 100;
  return parseFloat((weightKg / (heightM * heightM)).toFixed(1));
}

/** Return a descriptive BMI category string. */
export function getBMICategory(bmi: number): string {
  if (bmi < 18.5) return 'Underweight';
  if (bmi < 25) return 'Normal weight';
  if (bmi < 30) return 'Overweight';
  return 'Obese';
}
