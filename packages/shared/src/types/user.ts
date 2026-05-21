import { ActivityLevel, DietaryGoal, Gender } from '../enums';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  profileCompleted: boolean;
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
  profileCompleted: boolean;
}
