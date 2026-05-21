import { ActivityLevel, DietaryGoal, Gender } from '@/types';

/**
 * Calculate Basal Metabolic Rate using Mifflin-St Jeor equation.
 * weight in kg, height in cm, age in years.
 */
export function calculateBMR(
  weight: number,
  height: number,
  age: number,
  gender: Gender,
): number {
  const base = 10 * weight + 6.25 * height - 5 * age;
  if (gender === Gender.Male) {
    return Math.round(base + 5);
  }
  return Math.round(base - 161);
}

/**
 * Calculate Total Daily Energy Expenditure.
 */
export function calculateTDEE(bmr: number, activityLevel: ActivityLevel): number {
  const multipliers: Record<ActivityLevel, number> = {
    [ActivityLevel.Sedentary]: 1.2,
    [ActivityLevel.LightlyActive]: 1.375,
    [ActivityLevel.ModeratelyActive]: 1.55,
    [ActivityLevel.VeryActive]: 1.725,
    [ActivityLevel.ExtraActive]: 1.9,
  };
  return Math.round(bmr * multipliers[activityLevel]);
}

/**
 * Calculate macro targets in grams from total calories and dietary goal.
 */
export function calculateMacroTargets(
  calories: number,
  goal: DietaryGoal,
): { protein: number; carbs: number; fat: number } {
  const macroRatios: Record<DietaryGoal, { protein: number; carbs: number; fat: number }> = {
    [DietaryGoal.WeightLoss]: { protein: 0.35, carbs: 0.35, fat: 0.3 },
    [DietaryGoal.WeightMaintenance]: { protein: 0.25, carbs: 0.5, fat: 0.25 },
    [DietaryGoal.WeightGain]: { protein: 0.25, carbs: 0.5, fat: 0.25 },
    [DietaryGoal.MuscleGain]: { protein: 0.4, carbs: 0.4, fat: 0.2 },
    [DietaryGoal.GeneralHealth]: { protein: 0.25, carbs: 0.5, fat: 0.25 },
  };

  const ratios = macroRatios[goal];
  return {
    protein: Math.round((calories * ratios.protein) / 4), // 4 kcal/g
    carbs: Math.round((calories * ratios.carbs) / 4), // 4 kcal/g
    fat: Math.round((calories * ratios.fat) / 9), // 9 kcal/g
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
