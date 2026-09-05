<script setup lang="ts">
import { computed, ref } from 'vue';
import CustomFoodForm from '@/components/meal/CustomFoodForm.vue';
import { useDebouncedSearch } from '@/composables/useDebouncedSearch';
import { foodItemService } from '@/services/foodItemService';
import type { FoodItem } from '@foodeez/shared';

const emit = defineEmits<{ select: [item: FoodItem] }>();

const query = ref('');
const showCustomForm = ref(false);

const { results, onInput, reset } = useDebouncedSearch<FoodItem>((text) =>
  foodItemService.searchFoodItems(text),
);

/** Long enough to have searched for, so "nothing matched" means something. */
const hasSearched = computed(() => query.value.trim().length > 1);

function onQueryInput(): void {
  onInput(query.value);
}

function pick(item: FoodItem): void {
  emit('select', item);
  clear();
}

/** A freshly created homemade food goes straight into the meal. */
function onCustomFoodCreated(item: FoodItem): void {
  showCustomForm.value = false;
  pick(item);
}

function clear(): void {
  query.value = '';
  reset();
}

defineExpose({ clear });
</script>

<template>
  <div class="space-y-4">
    <div>
      <label
        for="food-search"
        class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200"
      >
        Or add one food at a time
      </label>
      <input
        id="food-search"
        v-model="query"
        type="text"
        placeholder="e.g. chicken breast, oatmeal..."
        class="w-full rounded-lg border border-gray-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600"
        @input="onQueryInput"
      />
    </div>

    <!-- Nothing matched: the food probably isn't in any database, so let them enter it. -->
    <button
      v-if="!showCustomForm && hasSearched && results.length === 0"
      type="button"
      class="flex w-full items-center gap-2 rounded-lg border border-dashed border-green-400 px-3 py-2.5 text-sm font-medium text-green-700 transition-colors hover:bg-green-50 dark:border-green-700 dark:text-green-400 dark:hover:bg-green-950/30"
      @click="showCustomForm = true"
    >
      <span class="text-base leading-none">+</span>
      Can't find "{{ query.trim() }}"? Add it as a homemade food
    </button>

    <CustomFoodForm
      v-if="showCustomForm"
      @created="onCustomFoodCreated"
      @cancel="showCustomForm = false"
    />

    <div
      v-if="results.length > 0"
      class="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700"
    >
      <button
        v-for="item in results"
        :key="item.id"
        type="button"
        class="flex w-full items-center justify-between border-b p-3 text-left transition-colors last:border-b-0 hover:bg-gray-50 dark:hover:bg-gray-800"
        @click="pick(item)"
      >
        <div>
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ item.name }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">
            {{ item.brand ? `${item.brand} · ` : ''
            }}{{ item.servingSize }}{{ item.servingUnit }} per serving
          </p>
        </div>
        <span class="ml-3 shrink-0 text-sm text-gray-500 dark:text-gray-400">
          {{ Math.round(item.nutritionalInfo.calories) }} kcal
        </span>
      </button>
    </div>

    <button
      v-if="!showCustomForm && results.length > 0"
      type="button"
      class="text-sm font-medium text-green-700 hover:underline dark:text-green-400"
      @click="showCustomForm = true"
    >
      None of these? Add a homemade food instead
    </button>
  </div>
</template>
