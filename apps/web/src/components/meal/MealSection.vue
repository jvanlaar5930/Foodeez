<script setup lang="ts">
import { ref } from 'vue';
import { MealType } from '@foodeez/shared';
import type { MealLogItem, NutritionalInfo } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';
import FoodItemRow from './FoodItemRow.vue';

interface Props {
  mealType: MealType;
  items: MealLogItem[];
  totalNutrition: NutritionalInfo;
}

defineProps<Props>();
const emit = defineEmits<{ deleteItem: [id: string] }>();

const isCollapsed = ref(false);

const mealIcons: Record<number, string> = {
  1: '🌅',
  2: '🍎',
  3: '🥗',
  4: '🥤',
  5: '🍽️',
  6: '🌙',
};
</script>

<template>
  <div class="bg-white rounded-xl border border-gray-100 overflow-hidden">
    <!-- Header -->
    <button
      class="w-full flex items-center justify-between px-4 py-3 hover:bg-gray-50 transition-colors"
      @click="isCollapsed = !isCollapsed"
    >
      <div class="flex items-center gap-2">
        <span class="text-lg">{{ mealIcons[mealType] ?? '🍴' }}</span>
        <span class="text-sm font-semibold text-gray-800">
          {{ MEAL_TYPE_LABELS[mealType] ?? `Meal ${mealType}` }}
        </span>
        <span class="text-xs text-gray-400 bg-gray-100 px-2 py-0.5 rounded-full">
          {{ items.length }} item{{ items.length !== 1 ? 's' : '' }}
        </span>
      </div>
      <div class="flex items-center gap-3">
        <span class="text-sm font-semibold text-green-700">
          {{ Math.round(totalNutrition.calories) }} kcal
        </span>
        <svg
          :class="['w-4 h-4 text-gray-400 transition-transform', isCollapsed ? '-rotate-90' : '']"
          fill="none" stroke="currentColor" viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </div>
    </button>

    <!-- Items -->
    <div v-if="!isCollapsed">
      <div v-if="items.length === 0" class="px-4 py-3 text-sm text-gray-400 italic">
        No items logged for this meal.
      </div>
      <div v-else class="divide-y divide-gray-50">
        <FoodItemRow
          v-for="item in items"
          :key="item.id"
          :item="item"
          class="px-1"
          @delete="(id) => emit('deleteItem', id)"
        />
      </div>

      <!-- Macro mini row -->
      <div v-if="items.length > 0" class="px-4 py-2 bg-gray-50 flex gap-4 text-xs text-gray-500">
        <span>P: {{ Math.round(totalNutrition.protein) }}g</span>
        <span>C: {{ Math.round(totalNutrition.carbohydrates) }}g</span>
        <span>F: {{ Math.round(totalNutrition.fat) }}g</span>
      </div>
    </div>
  </div>
</template>
