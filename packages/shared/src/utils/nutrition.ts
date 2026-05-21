import { ACTIVITY_LEVEL_MULTIPLIERS, GOAL_CALORIE_ADJUSTMENTS, MACRO_GOAL_PERCENTAGES } from '../constants/nutrition';

/**
 * Calculate Basal Metabolic Rate using the Mifflin-St Jeor equation.
 * @param weightKg - weight in kilograms
 * @param heightCm - height in centimetres
 * @param age - age in years
 * @param gender - 'Male' | 'Female' | other (treated as Female formula)
 * @returns BMR in kcal/day
 */
export function calculateBMR(
  weightKg: number,
  heightCm: number,
  age: number,
  gender: string,
): number {
  // Mifflin-St Jeor
  const base = 10 * weightKg + 6.25 * heightCm - 5 * age;
  return gender === 'Male' ? base + 5 : base - 161;
}

/**
 * Calculate Total Daily Energy Expenditure.
 * @param bmr - Basal Metabolic Rate in kcal/day
 * @param activityLevel - ActivityLevel enum value string
 * @returns TDEE in kcal/day, rounded to the nearest integer
 */
export function calculateTDEE(bmr: number, activityLevel: string): number {
  const multiplier =
    (ACTIVITY_LEVEL_MULTIPLIERS as Record<string, number>)[activityLevel] ?? 1.2;
  return Math.round(bmr * multiplier);
}

/**
 * Calculate daily macro gram targets based on calorie goal and dietary goal.
 * @param calories - total daily calorie target
 * @param goal - DietaryGoal enum value string
 * @returns grams of protein, carbs, and fat
 */
export function calculateMacroTargets(
  calories: number,
  goal: string,
): { proteinG: number; carbsG: number; fatG: number } {
  const percentages =
    (MACRO_GOAL_PERCENTAGES as Record<string, { protein: number; carbs: number; fat: number }>)[
      goal
    ] ?? MACRO_GOAL_PERCENTAGES.GeneralHealth;

  return {
    proteinG: Math.round((calories * percentages.protein) / 4),
    carbsG: Math.round((calories * percentages.carbs) / 4),
    fatG: Math.round((calories * percentages.fat) / 9),
  };
}

/**
 * Format a nutrition value with its unit for display.
 * @param value - numeric value
 * @param unit - unit string (e.g. 'g', 'mg', 'kcal')
 * @returns formatted string like "42 g"
 */
export function formatNutritionValue(value: number, unit: string): string {
  const rounded = Number.isInteger(value) ? value : parseFloat(value.toFixed(1));
  return `${rounded} ${unit}`;
}

/**
 * Calculate calorie target adjusted for dietary goal.
 * @param tdee - Total Daily Energy Expenditure
 * @param goal - DietaryGoal enum value string
 * @returns adjusted daily calorie target
 */
export function calculateCalorieTarget(tdee: number, goal: string): number {
  const adjustment =
    (GOAL_CALORIE_ADJUSTMENTS as Record<string, number>)[goal] ?? 0;
  return Math.max(1200, tdee + adjustment);
}

/**
 * Calculate Body Mass Index.
 * @param weightKg - weight in kilograms
 * @param heightCm - height in centimetres
 * @returns BMI value rounded to one decimal place
 */
export function calculateBMI(weightKg: number, heightCm: number): number {
  const heightM = heightCm / 100;
  return parseFloat((weightKg / (heightM * heightM)).toFixed(1));
}

/**
 * Return a descriptive BMI category string.
 */
export function getBMICategory(bmi: number): string {
  if (bmi < 18.5) return 'Underweight';
  if (bmi < 25) return 'Normal weight';
  if (bmi < 30) return 'Overweight';
  return 'Obese';
}
