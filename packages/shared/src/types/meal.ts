import { MealType } from '../enums';
import { NutritionalInfo } from './nutrition';

export interface FoodItem {
  id: string;
  name: string;
  brand?: string;
  servingSize: number;
  servingUnit: string;
  category?: string;
  barcode?: string;
  isCustom: boolean;
  nutritionalInfo: NutritionalInfo;
}

export interface MealLogItem {
  id: string;
  mealLogId: string;
  foodItem: FoodItem;
  quantity: number;
  unit: string;
  nutritionalInfo: NutritionalInfo;
}

export interface MealLog {
  id: string;
  userId: string;
  logDate: string;
  mealType: MealType;
  notes?: string;
  items: MealLogItem[];
  totalNutrition: NutritionalInfo;
  createdAt: string;
}
