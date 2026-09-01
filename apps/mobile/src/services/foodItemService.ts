import { api } from './api';
import type { FoodItemDto } from '@/types';

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
