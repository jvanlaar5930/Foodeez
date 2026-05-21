<script setup lang="ts">
import { computed } from 'vue';
import { MealType } from '@foodeez/shared';
import type { MealPlanEntry } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';

interface Props {
  date: Date;
  mealType: MealType;
  entry?: MealPlanEntry;
}

const props = defineProps<Props>();
const emit = defineEmits<{ click: [] }>();

const displayName = computed(() => {
  if (!props.entry) return null;
  return props.entry.recipeName ?? props.entry.foodItemName ?? null;
});

const mealColors: Record<number, string> = {
  1: 'bg-orange-100 text-orange-700 border-orange-200',
  2: 'bg-green-100 text-green-700 border-green-200',
  3: 'bg-blue-100 text-blue-700 border-blue-200',
  4: 'bg-yellow-100 text-yellow-700 border-yellow-200',
  5: 'bg-purple-100 text-purple-700 border-purple-200',
  6: 'bg-pink-100 text-pink-700 border-pink-200',
};
</script>

<template>
  <button
    :title="displayName ?? `Add ${MEAL_TYPE_LABELS[mealType]}`"
    :class="[
      'w-full h-full min-h-[3rem] p-1 rounded-md text-left transition-colors text-xs',
      entry
        ? `${mealColors[mealType] ?? 'bg-gray-100 text-gray-700 border-gray-200'} border`
        : 'text-gray-300 hover:bg-gray-50 hover:text-gray-500',
    ]"
    @click="emit('click')"
  >
    <span v-if="displayName" class="line-clamp-2 font-medium leading-snug">
      {{ displayName }}
    </span>
    <span v-else class="flex items-center justify-center h-full opacity-0 hover:opacity-100 transition-opacity">
      +
    </span>
  </button>
</template>
