import type {
  GenerateGroceryListRequest,
  GroceryItem,
  GroceryItemRequest,
  GroceryList,
  GroceryListState,
} from '@foodeez/shared';

import { streamAI } from './aiStream';
import api from './api';

export const groceryService = {
  /**
   * The list stored for a range, plus how many meals are planned in it. Costs no AI call,
   * so it is safe on every load and every change of range.
   */
  async getList(startDate: string, endDate: string): Promise<GroceryListState> {
    const response = await api.get<GroceryListState>('/grocery-lists', {
      params: { startDate, endDate },
    });
    return response.data;
  },

  /**
   * Compile the list, streaming as the model writes. A list already compiled from exactly
   * these meals arrives complete with no deltas; only `refresh` spends another AI call.
   */
  async generate(
    request: GenerateGroceryListRequest,
    onDelta: (text: string) => void,
    signal?: AbortSignal,
  ): Promise<GroceryList> {
    return streamAI<GroceryList>('/grocery-lists/generate/stream', { body: request, signal }, onDelta);
  },

  async addItem(listId: string, item: GroceryItemRequest): Promise<GroceryItem> {
    const response = await api.post<GroceryItem>(`/grocery-lists/${listId}/items`, item);
    return response.data;
  },

  async updateItem(listId: string, itemId: string, item: GroceryItemRequest): Promise<GroceryItem> {
    const response = await api.put<GroceryItem>(`/grocery-lists/${listId}/items/${itemId}`, item);
    return response.data;
  },

  /** Ticking one line off, without resending the rest of it. */
  async setChecked(listId: string, itemId: string, isChecked: boolean): Promise<GroceryItem> {
    const response = await api.patch<GroceryItem>(
      `/grocery-lists/${listId}/items/${itemId}/checked`,
      { isChecked },
    );
    return response.data;
  },

  async deleteItem(listId: string, itemId: string): Promise<void> {
    await api.delete(`/grocery-lists/${listId}/items/${itemId}`);
  },
};
