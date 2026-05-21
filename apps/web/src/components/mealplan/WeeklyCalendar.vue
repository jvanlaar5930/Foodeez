<script setup lang="ts">
import { computed } from 'vue';
import { MealType } from '@foodeez/shared';
import type { MealPlanEntry } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';
import { format, isToday } from 'date-fns';
import DayMealSlot from './DayMealSlot.vue';

interface Props {
  weekDays: Date[];
  entries: MealPlanEntry[];
}

const props = defineProps<Props>();
const emit = defineEmits<{
  'cell-click': [payload: { date: Date; mealType: MealType }];
}>();

const mealTypes = [
  MealType.Breakfast,
  MealType.MorningSnack,
  MealType.Lunch,
  MealType.AfternoonSnack,
  MealType.Dinner,
  MealType.EveningSnack,
];

function getEntry(date: Date, mealType: MealType): MealPlanEntry | undefined {
  const dateStr = format(date, 'yyyy-MM-dd');
  return props.entries.find(
    (e) => e.entryDate === dateStr && e.mealType === mealType,
  );
}
</script>

<template>
  <div class="overflow-x-auto">
    <div class="min-w-[640px]">
      <!-- Header row: meal label + day columns -->
      <div class="grid gap-px" :style="{ gridTemplateColumns: '80px repeat(7, 1fr)' }">
        <!-- top-left spacer -->
        <div class="h-12" />
        <!-- Day headers -->
        <div
          v-for="day in weekDays"
          :key="day.toISOString()"
          :class="[
            'h-12 flex flex-col items-center justify-center text-xs font-semibold rounded-t-lg',
            isToday(day)
              ? 'bg-green-600 text-white'
              : 'bg-gray-100 text-gray-600',
          ]"
        >
          <span>{{ format(day, 'EEE') }}</span>
          <span :class="['text-lg leading-tight', isToday(day) ? 'font-bold' : '']">
            {{ format(day, 'd') }}
          </span>
        </div>
      </div>

      <!-- Meal rows -->
      <div
        v-for="mealType in mealTypes"
        :key="mealType"
        class="grid gap-px border-t border-gray-100"
        :style="{ gridTemplateColumns: '80px repeat(7, 1fr)' }"
      >
        <!-- Meal label -->
        <div class="py-2 pr-2 flex items-center">
          <span class="text-xs font-medium text-gray-500 leading-tight">
            {{ MEAL_TYPE_LABELS[mealType] }}
          </span>
        </div>

        <!-- Day cells -->
        <div
          v-for="day in weekDays"
          :key="day.toISOString()"
          class="py-1 px-0.5 min-h-[3.5rem]"
        >
          <DayMealSlot
            :date="day"
            :meal-type="mealType"
            :entry="getEntry(day, mealType)"
            @click="emit('cell-click', { date: day, mealType })"
          />
        </div>
      </div>
    </div>
  </div>
</template>
