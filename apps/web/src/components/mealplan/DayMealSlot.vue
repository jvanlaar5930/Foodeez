<script setup lang="ts">
import { computed } from 'vue';
import { MealType } from '@foodeez/shared';
import type { MealPlanEntry } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';

interface Props {
  date: Date;
  mealType: MealType;
  entry?: MealPlanEntry;
  /** What was actually logged for this slot, if anything - read-only, from the Meal Log. */
  loggedLabel?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{ click: [] }>();

const displayName = computed(() => {
  if (!props.entry) return null;
  // AI-generated entries have no Recipe or FoodItem row behind them, so the meal's own
  // name is in `notes` - without this fallback every generated slot rendered nameless.
  return props.entry.recipeName ?? props.entry.foodItemName ?? props.entry.notes ?? null;
});

const mealColors: Record<MealType, string> = {
  [MealType.Breakfast]: 'bg-orange-100 dark:bg-orange-900/40 text-orange-700 dark:text-orange-400 border-orange-200',
  [MealType.MorningSnack]: 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400 border-green-200 dark:border-green-800',
  [MealType.Lunch]: 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-400 border-blue-200 dark:border-blue-900/50',
  [MealType.AfternoonSnack]: 'bg-yellow-100 dark:bg-yellow-900/40 text-yellow-700 dark:text-yellow-400 border-yellow-200 dark:border-yellow-900/50',
  [MealType.Dinner]: 'bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-400 border-purple-200',
  [MealType.EveningSnack]: 'bg-pink-100 dark:bg-pink-900/40 text-pink-700 border-pink-200',
};
</script>

<template>
  <button
    :title="displayName ?? loggedLabel ?? `Add ${MEAL_TYPE_LABELS[mealType]}`"
    :class="[
      'w-full h-full min-h-[3rem] p-1 rounded-md text-left transition-colors text-xs',
      entry
        ? `${mealColors[mealType] ?? 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-200 border-gray-200 dark:border-gray-700'} border`
        : loggedLabel
          ? 'border border-dashed border-gray-300 text-gray-500 dark:border-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800'
          : 'text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 hover:text-gray-500 dark:hover:text-gray-400',
    ]"
    @click="emit('click')"
  >
    <span v-if="displayName" class="line-clamp-2 font-medium leading-snug">
      {{ displayName }}
      <span v-if="loggedLabel" title="Also logged as eaten">✓</span>
    </span>
    <span v-else-if="loggedLabel" class="line-clamp-2 italic leading-snug">
      {{ loggedLabel }}
    </span>
    <span v-else class="flex items-center justify-center h-full opacity-0 hover:opacity-100 transition-opacity">
      +
    </span>
  </button>
</template>
