<script setup lang="ts">
import { ref } from 'vue';
import { MEAL_TYPE_LABELS, MEAL_TYPE_SHORT_LABELS, MealType } from '@foodeez/shared';
import type { MealLog, MealLogItem, NutritionalInfo } from '@foodeez/shared';
import FoodItemRow from './FoodItemRow.vue';
import MealAnalysisBadge from './MealAnalysisBadge.vue';

interface Props {
  mealLog: MealLog;
  mealType: MealType;
  items: MealLogItem[];
  totalNutrition: NutritionalInfo;
}

defineProps<Props>();
const emit = defineEmits<{ editMeal: [mealLog: MealLog]; deleteMeal: [mealLog: MealLog] }>();

const isCollapsed = ref(false);
</script>

<template>
  <div class="overflow-hidden rounded-xl border border-gray-100 dark:border-gray-800 bg-white dark:bg-gray-900">
    <div class="flex items-center justify-between px-4 py-3 transition-colors hover:bg-gray-50 dark:hover:bg-gray-800">
      <div class="flex items-center gap-2">
        <span class="text-sm font-semibold text-gray-500 dark:text-gray-400">{{ MEAL_TYPE_SHORT_LABELS[mealType] ?? 'Meal' }}</span>
        <span class="text-sm font-semibold text-gray-800 dark:text-gray-100">
          {{ MEAL_TYPE_LABELS[mealType] ?? `Meal ${mealType}` }}
        </span>
        <span class="rounded-full bg-gray-100 dark:bg-gray-800 px-2 py-0.5 text-xs text-gray-400">
          {{ items.length }} item{{ items.length !== 1 ? 's' : '' }}
        </span>
        <!-- Only for a meal that already has an analysis stored: this reads what is on file
             and never spends an AI call, so it is safe to show on every row. -->
        <MealAnalysisBadge
          v-if="mealLog.analysis"
          :analysis="mealLog.analysis"
          :meal-type="mealType"
        />
      </div>
      <div class="flex items-center gap-3">
        <span class="text-sm font-semibold text-green-700 dark:text-green-400">
          {{ Math.round(totalNutrition.calories) }} kcal
        </span>
        <button
          class="text-xs font-medium text-green-700 dark:text-green-400 hover:text-green-800 dark:hover:text-green-300"
          @click.stop="emit('editMeal', mealLog)"
        >
          Edit
        </button>
        <button
          class="text-xs font-medium text-red-500 dark:text-red-400 hover:text-red-600 dark:hover:text-red-400"
          @click.stop="emit('deleteMeal', mealLog)"
        >
          Delete
        </button>
        <button
          class="rounded p-1 text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
          @click="isCollapsed = !isCollapsed"
        >
          <svg
            :class="['h-4 w-4 transition-transform', isCollapsed ? '-rotate-90' : '']"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
        </button>
      </div>
    </div>

    <div v-if="!isCollapsed">
      <div v-if="items.length === 0" class="px-4 py-3 text-sm italic text-gray-400">
        No items logged for this meal.
      </div>
      <div v-else class="divide-y divide-gray-50">
        <FoodItemRow
          v-for="item in items"
          :key="item.id"
          :item="item"
          class="px-1"
        />
      </div>

      <div v-if="items.length > 0" class="flex gap-4 bg-gray-50 dark:bg-gray-800 px-4 py-2 text-xs text-gray-500 dark:text-gray-400">
        <span>P: {{ Math.round(totalNutrition.protein) }}g</span>
        <span>C: {{ Math.round(totalNutrition.carbohydrates) }}g</span>
        <span>F: {{ Math.round(totalNutrition.fat) }}g</span>
      </div>
    </div>
  </div>
</template>
