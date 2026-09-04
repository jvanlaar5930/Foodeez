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
  /** Original publisher, for attribution and for recipes whose method lives off-site. */
  sourceUrl?: string;
  sourceName?: string;
  /** False when the upstream source has no method for this recipe at all. */
  hasInstructions?: boolean;
  /** True when the method could not be fetched this time - worth retrying, unlike the above. */
  detailUnavailable?: boolean;
  ingredients: RecipeIngredient[];
  nutritionalInfoPerServing: NutritionalInfo;
  /** Who saved it, when it was not one of the built-in ones. Null for library recipes. */
  createdByUserId?: string;
  createdAt: string;
}
