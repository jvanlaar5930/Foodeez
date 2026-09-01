import api from './api';
import type { FoodItem } from '@foodeez/shared';

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

export const foodItemService = {
  async searchFoodItems(query: string): Promise<FoodItem[]> {
    if (!query.trim()) return [];
    const response = await api.get<FoodItem[]>('/food-items/search', {
      params: { q: query },
    });
    return response.data;
  },

  async createFoodItem(data: CreateFoodItemRequest): Promise<FoodItem> {
    const response = await api.post<FoodItem>('/food-items', data);
    return response.data;
  },

  async getFoodItemById(id: string): Promise<FoodItem> {
    const response = await api.get<FoodItem>(`/food-items/${id}`);
    return response.data;
  },

  async getFoodItemByBarcode(barcode: string): Promise<FoodItem> {
    const response = await api.get<FoodItem>(`/food-items/barcode/${barcode}`);
    return response.data;
  },
};
