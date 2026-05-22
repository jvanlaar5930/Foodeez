import { NutritionalInfo } from './nutrition';

export interface RecipeIngredient {
  id: string;
  foodItemId?: string;
  foodItemName: string;
  quantity: number;
  unit: string;
  notes?: string;
}

export interface Recipe {
  id: string;
  name: string;
  description?: string;
  instructions: string;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  tags?: string;
  imageUrl?: string;
  isAIGenerated: boolean;
  ingredients: RecipeIngredient[];
  nutritionalInfoPerServing: NutritionalInfo;
  createdAt: string;
}
