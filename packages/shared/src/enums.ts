export enum Gender {
  Male = 'Male',
  Female = 'Female',
  Other = 'Other',
  PreferNotToSay = 'PreferNotToSay',
}

export enum ActivityLevel {
  Sedentary = 'Sedentary',
  LightlyActive = 'LightlyActive',
  ModeratelyActive = 'ModeratelyActive',
  VeryActive = 'VeryActive',
  ExtraActive = 'ExtraActive',
}

export enum DietaryGoal {
  WeightLoss = 'WeightLoss',
  WeightMaintenance = 'WeightMaintenance',
  WeightGain = 'WeightGain',
  MuscleGain = 'MuscleGain',
  GeneralHealth = 'GeneralHealth',
}

/** Serialised by the API as the C# member name, so these values must match exactly. */
export enum MealType {
  Breakfast = 'Breakfast',
  MorningSnack = 'MorningSnack',
  Lunch = 'Lunch',
  AfternoonSnack = 'AfternoonSnack',
  Dinner = 'Dinner',
  EveningSnack = 'EveningSnack',
}

/** Serialised by the API as the C# member name, so these values must match exactly. */
export enum UnitSystem {
  US = 'US',
  Metric = 'Metric',
}
