import { api } from './api';
import type { FoodItemDto } from '@/types';

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
