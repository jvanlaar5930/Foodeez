import { api } from './api';
import type { FoodItemDto, RecipeDto } from '@/types';

/** A homemade or otherwise unlisted food. Ownership is taken from the token server-side. */
export interface CreateFoodItemRequest {
  name: string;
  brand?: string;
  servingSize: number;
  servingUnit: string;
  category?: string;
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
  fiber: number;
  sugar: number;
  sodium: number;
}

export async function createFoodItem(data: CreateFoodItemRequest): Promise<FoodItemDto> {
  const response = await api.post<FoodItemDto>('/food-items', data);
  return response.data;
}

export async function searchFoodItems(query: string): Promise<FoodItemDto[]> {
  if (!query.trim()) return [];
  const response = await api.get<FoodItemDto[]>('/food-items/search', {
    params: { q: query },
  });
  return response.data;
}

export async function getFoodItemById(id: string): Promise<FoodItemDto> {
  const response = await api.get<FoodItemDto>(`/food-items/${id}`);
  return response.data;
}

export async function getPopularFoodItems(): Promise<FoodItemDto[]> {
  const response = await api.get<FoodItemDto[]>('/food-items/popular');
  return response.data;
}

/**
 * The food item standing for one serving of a recipe, so a recipe can be logged the same way
 * anything else is - meal logs are made of food items, and a recipe is not one.
 *
 * An existing item of the same name is reused rather than a second one created every time
 * the recipe is logged.
 */
export async function foodItemForRecipe(recipe: RecipeDto): Promise<FoodItemDto> {
  const n = recipe.nutritionalInfoPerServing;

  try {
    const matches = await searchFoodItems(recipe.name);
    const existing = matches.find(
      (item) => item.name.trim().toLowerCase() === recipe.name.trim().toLowerCase(),
    );
    if (existing) return existing;
  } catch {
    // Search is only there to avoid a duplicate; failing it should not block the log.
  }

  return createFoodItem({
    name: recipe.name,
    servingSize: 1,
    servingUnit: 'serving',
    category: 'Recipe',
    calories: n.calories,
    protein: n.protein,
    carbohydrates: n.carbohydrates,
    fat: n.fat,
    fiber: n.fiber,
    sugar: n.sugar,
    sodium: n.sodium,
  });
}
