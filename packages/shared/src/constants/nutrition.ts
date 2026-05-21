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
  MuscleGain: 250,
  GeneralHealth: 0,
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

export const MACRO_GOAL_PERCENTAGES = {
  WeightLoss: { protein: 0.35, carbs: 0.35, fat: 0.30 },
  WeightMaintenance: { protein: 0.25, carbs: 0.50, fat: 0.25 },
  WeightGain: { protein: 0.25, carbs: 0.50, fat: 0.25 },
  MuscleGain: { protein: 0.40, carbs: 0.35, fat: 0.25 },
  GeneralHealth: { protein: 0.25, carbs: 0.50, fat: 0.25 },
} as const;

export const MEAL_TYPE_LABELS: Record<number, string> = {
  1: 'Breakfast',
  2: 'Morning Snack',
  3: 'Lunch',
  4: 'Afternoon Snack',
  5: 'Dinner',
  6: 'Evening Snack',
};
