import { create } from 'zustand';
import * as groceryService from '@/services/groceryService';
import { AIStreamError } from '@/services/aiStream';
import { describeApiError } from '@/utils/apiError';
import type { GroceryItemRequest, GroceryListDto } from '@/types';

interface GroceryState {
  list: GroceryListDto | null;
  /** How many meals are planned in the current range, whether or not a list exists yet. */
  plannedMealCount: number;
  isLoading: boolean;
  isGenerating: boolean;
  /** What the model has written so far this build, for the screen to show as it arrives. */
  generationText: string;
  error: string | null;

  load: (startDate: string, endDate: string) => Promise<void>;
  generate: (startDate: string, endDate: string, refresh?: boolean) => Promise<void>;
  cancelGenerate: () => void;
  addItem: (item: GroceryItemRequest) => Promise<void>;
  updateItem: (itemId: string, item: GroceryItemRequest) => Promise<void>;
  setChecked: (itemId: string, isChecked: boolean) => Promise<void>;
  removeItem: (itemId: string) => Promise<void>;
  clearError: () => void;
}

let inFlight: { cancel: () => void } | null = null;

export const useGroceryStore = create<GroceryState>()((set, get) => ({
  list: null,
  plannedMealCount: 0,
  isLoading: false,
  isGenerating: false,
  generationText: '',
  error: null,

  load: async (startDate: string, endDate: string) => {
    set({ isLoading: true, error: null });
    try {
      const state = await groceryService.getList(startDate, endDate);
      set({ list: state.list, plannedMealCount: state.plannedMealCount, isLoading: false });
    } catch (err: unknown) {
      set({ isLoading: false, error: describeApiError(err, 'Your grocery list could not be loaded.') });
    }
  },

  generate: async (startDate: string, endDate: string, refresh = false) => {
    if (get().isGenerating) {
      return;
    }

    set({ isGenerating: true, error: null, generationText: '' });

    const request = groceryService.generate({ startDate, endDate, refresh }, (delta) =>
      set((state) => ({ generationText: state.generationText + delta })),
    );
    inFlight = request;

    try {
      const list = await request;
      set({ list, plannedMealCount: list.plannedMealCount });
    } catch (err: unknown) {
      set({
        error:
          err instanceof AIStreamError
            ? err.message
            : describeApiError(err, 'The list could not be put together. Please try again.'),
      });
    } finally {
      inFlight = null;
      set({ isGenerating: false, generationText: '' });
    }
  },

  /** Aborting closes the connection, which is what actually stops the model call. */
  cancelGenerate: () => {
    inFlight?.cancel();
    inFlight = null;
  },

  addItem: async (item: GroceryItemRequest) => {
    const list = get().list;
    if (!list) {
      return;
    }

    try {
      const added = await groceryService.addItem(list.id, item);
      set({ list: { ...list, items: [...list.items, added] } });
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That item could not be added.') });
    }
  },

  updateItem: async (itemId: string, item: GroceryItemRequest) => {
    const list = get().list;
    if (!list) {
      return;
    }

    try {
      const updated = await groceryService.updateItem(list.id, itemId, item);
      set({
        list: { ...list, items: list.items.map((i) => (i.id === itemId ? updated : i)) },
      });
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That change could not be saved.') });
    }
  },

  /**
   * Ticks the box on screen first and puts it back if the save fails - a checklist that waits
   * for a round trip before it responds is unusable in a shop.
   */
  setChecked: async (itemId: string, isChecked: boolean) => {
    const list = get().list;
    const item = list?.items.find((i) => i.id === itemId);
    if (!list || !item) {
      return;
    }

    const previous = item.isChecked;
    const apply = (value: boolean) =>
      set((state) => ({
        list: state.list
          ? {
              ...state.list,
              items: state.list.items.map((i) => (i.id === itemId ? { ...i, isChecked: value } : i)),
            }
          : null,
      }));

    apply(isChecked);

    try {
      await groceryService.setChecked(list.id, itemId, isChecked);
    } catch (err: unknown) {
      apply(previous);
      set({ error: describeApiError(err, 'That could not be saved.') });
    }
  },

  removeItem: async (itemId: string) => {
    const list = get().list;
    if (!list) {
      return;
    }

    try {
      await groceryService.deleteItem(list.id, itemId);
      set({ list: { ...list, items: list.items.filter((i) => i.id !== itemId) } });
    } catch (err: unknown) {
      set({ error: describeApiError(err, 'That item could not be removed.') });
    }
  },

  clearError: () => set({ error: null }),
}));
