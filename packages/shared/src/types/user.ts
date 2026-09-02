import { ActivityLevel, DietaryGoal, Gender, UnitSystem } from '../enums';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileCompleted: boolean;
  isAdmin: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface UserProfile {
  userId: string;
  heightCm: number;
  weightKg: number;
  targetWeightKg?: number;
  age: number;
  gender: Gender;
  activityLevel: ActivityLevel;
  dietaryGoal: DietaryGoal;
  dailyCalorieTarget: number;
  dailyProteinTargetG: number;
  dailyCarbTargetG: number;
  dailyFatTargetG: number;
  notes?: string;
  /** Foods to keep out of every AI suggestion - allergies, intolerances, dislikes. */
  excludedFoods: string[];
  profileCompleted: boolean;
  darkMode: boolean;
  unitSystem: UnitSystem;
}
