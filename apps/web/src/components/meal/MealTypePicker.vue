<script setup lang="ts">
import { MEAL_TYPE_SHORT_LABELS, MealType } from '@foodeez/shared';

defineProps<{ modelValue: MealType }>();
const emit = defineEmits<{ 'update:modelValue': [value: MealType] }>();

/**
 * In the order a day happens, so the row reads as a timeline. The labels come from the shared
 * package - four disagreeing abbreviated sets used to exist across the two clients, which is
 * how the evening slot ended up being called three different things.
 */
const MEAL_TYPES: MealType[] = [
  MealType.Breakfast,
  MealType.MorningSnack,
  MealType.Lunch,
  MealType.AfternoonSnack,
  MealType.Dinner,
  MealType.EveningSnack,
];
</script>

<template>
  <div>
    <label class="mb-2 block text-sm font-medium text-gray-700 dark:text-gray-200">Meal Type</label>
    <div class="flex flex-wrap gap-2">
      <button
        v-for="mealType in MEAL_TYPES"
        :key="mealType"
        type="button"
        class="rounded-full border px-3 py-1.5 text-sm font-medium transition-colors"
        :class="
          modelValue === mealType
            ? 'border-green-600 bg-green-600 text-white'
            : 'border-gray-300 text-gray-600 hover:border-green-400 dark:border-gray-600 dark:text-gray-300'
        "
        :aria-pressed="modelValue === mealType"
        @click="emit('update:modelValue', mealType)"
      >
        {{ MEAL_TYPE_SHORT_LABELS[mealType] }}
      </button>
    </div>
  </div>
</template>
