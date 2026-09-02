import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import type { GroceryItem, GroceryItemRequest, GroceryList } from '@foodeez/shared';
import { groceryService } from '@/services/groceryService';
import { extractErrorMessage } from '@/utils/apiError';
import { GROCERY_CATEGORIES } from '@foodeez/shared';

export const useGroceryStore = defineStore('grocery', () => {
  const list = ref<GroceryList | null>(null);
  /** How many meals are planned in the current range, whether or not a list exists yet. */
  const plannedMealCount = ref(0);
  const isLoading = ref(false);
  const isGenerating = ref(false);
  /** What the model has written so far this build, for the view to show as it arrives. */
  const generationText = ref('');
  const error = ref<string | null>(null);

  /** Aisle order, so the list is walked rather than hunted through. */
  const grouped = computed(() => {
    const items = list.value?.items ?? [];

    return GROCERY_CATEGORIES.map((category) => ({
      category,
      items: items.filter((item) => item.category === category),
    })).filter((group) => group.items.length > 0);
  });

  const remainingCount = computed(
    () => (list.value?.items ?? []).filter((item) => !item.isChecked).length,
  );

  let generateController: AbortController | null = null;

  async function load(startDate: string, endDate: string): Promise<void> {
    isLoading.value = true;
    error.value = null;
    try {
      const state = await groceryService.getList(startDate, endDate);
      list.value = state.list;
      plannedMealCount.value = state.plannedMealCount;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'Your grocery list could not be loaded.');
    } finally {
      isLoading.value = false;
    }
  }

  async function generate(startDate: string, endDate: string, refresh = false): Promise<void> {
    if (isGenerating.value) {
      return;
    }

    isGenerating.value = true;
    error.value = null;
    generationText.value = '';
    generateController = new AbortController();

    try {
      list.value = await groceryService.generate(
        { startDate, endDate, refresh },
        (delta) => {
          generationText.value += delta;
        },
        generateController.signal,
      );
      plannedMealCount.value = list.value.plannedMealCount;
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'The list could not be put together. Please try again.');
    } finally {
      isGenerating.value = false;
      generationText.value = '';
      generateController = null;
    }
  }

  /** Aborting closes the connection, which is what actually stops the model call. */
  function cancelGenerate(): void {
    generateController?.abort();
    generateController = null;
  }

  async function addItem(item: GroceryItemRequest): Promise<void> {
    if (!list.value) {
      return;
    }

    error.value = null;
    try {
      const added = await groceryService.addItem(list.value.id, item);
      list.value.items.push(added);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'That item could not be added.');
    }
  }

  async function updateItem(itemId: string, item: GroceryItemRequest): Promise<void> {
    if (!list.value) {
      return;
    }

    error.value = null;
    try {
      const updated = await groceryService.updateItem(list.value.id, itemId, item);
      replace(updated);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'That change could not be saved.');
    }
  }

  /**
   * Ticks the box on screen first and puts it back if the save fails - a checklist that waits
   * for a round trip before it responds is unusable in a shop.
   */
  async function setChecked(itemId: string, isChecked: boolean): Promise<void> {
    const item = list.value?.items.find((i) => i.id === itemId);
    if (!list.value || !item) {
      return;
    }

    const previous = item.isChecked;
    item.isChecked = isChecked;

    try {
      await groceryService.setChecked(list.value.id, itemId, isChecked);
    } catch (err: unknown) {
      item.isChecked = previous;
      error.value = extractErrorMessage(err, 'That could not be saved.');
    }
  }

  async function removeItem(itemId: string): Promise<void> {
    if (!list.value) {
      return;
    }

    error.value = null;
    try {
      await groceryService.deleteItem(list.value.id, itemId);
      list.value.items = list.value.items.filter((i) => i.id !== itemId);
    } catch (err: unknown) {
      error.value = extractErrorMessage(err, 'That item could not be removed.');
    }
  }

  function clearError(): void {
    error.value = null;
  }

  function replace(item: GroceryItem): void {
    if (!list.value) {
      return;
    }

    const index = list.value.items.findIndex((i) => i.id === item.id);
    if (index >= 0) {
      list.value.items[index] = item;
    }
  }

  return {
    list,
    plannedMealCount,
    grouped,
    remainingCount,
    isLoading,
    isGenerating,
    generationText,
    error,
    load,
    generate,
    cancelGenerate,
    addItem,
    updateItem,
    setChecked,
    removeItem,
    clearError,
  };
});
