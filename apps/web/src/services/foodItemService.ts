import api from './api';
import type { FoodItem } from '@foodeez/shared';

export const foodItemService = {
  async searchFoodItems(query: string): Promise<FoodItem[]> {
    if (!query.trim()) return [];
    const response = await api.get<FoodItem[]>('/food-items/search', {
      params: { q: query },
    });
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
