import { api } from './api';
import { authToken, streamAI, type StreamHandle } from './aiStream';
import type {
  GenerateGroceryListRequest,
  GroceryItemDto,
  GroceryItemRequest,
  GroceryListDto,
  GroceryListStateDto,
} from '@/types';

/**
 * The list stored for a range, plus how many meals are planned in it. Costs no AI call, so
 * it is safe on every load and every change of range.
 */
export async function getList(startDate: string, endDate: string): Promise<GroceryListStateDto> {
  const response = await api.get<GroceryListStateDto>('/grocery-lists', {
    params: { startDate, endDate },
  });
  return response.data;
}

/**
 * Compile the list, streaming as the model writes. A list already compiled from exactly these
 * meals arrives complete with no deltas; only `refresh` spends another AI call.
 */
export function generate(
  request: GenerateGroceryListRequest,
  onDelta: (text: string) => void,
): Promise<GroceryListDto> & StreamHandle {
  return streamAI<GroceryListDto>(
    '/grocery-lists/generate/stream',
    { body: request, token: authToken() },
    onDelta,
  );
}

export async function addItem(listId: string, item: GroceryItemRequest): Promise<GroceryItemDto> {
  const response = await api.post<GroceryItemDto>(`/grocery-lists/${listId}/items`, item);
  return response.data;
}

export async function updateItem(
  listId: string,
  itemId: string,
  item: GroceryItemRequest,
): Promise<GroceryItemDto> {
  const response = await api.put<GroceryItemDto>(`/grocery-lists/${listId}/items/${itemId}`, item);
  return response.data;
}

export async function setChecked(
  listId: string,
  itemId: string,
  isChecked: boolean,
): Promise<GroceryItemDto> {
  const response = await api.patch<GroceryItemDto>(
    `/grocery-lists/${listId}/items/${itemId}/checked`,
    { isChecked },
  );
  return response.data;
}

export async function deleteItem(listId: string, itemId: string): Promise<void> {
  await api.delete(`/grocery-lists/${listId}/items/${itemId}`);
}
