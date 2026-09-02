<script setup lang="ts">
import { computed, ref } from 'vue';
import type { PlannedMeal } from '@foodeez/shared';

const props = defineProps<{
  suggestions: PlannedMeal[];
  /** Set once these meals are on the calendar. */
  acceptedAt?: string;
  canAdd?: boolean;
}>();

const emit = defineEmits<{ add: [] }>();

const isAdding = ref(false);

const isAccepted = computed(() => Boolean(props.acceptedAt));

/** Grouped by day, because that is how a week of meals is read. */
const byDate = computed(() => {
  const groups = new Map<string, PlannedMeal[]>();

  for (const meal of props.suggestions) {
    const existing = groups.get(meal.date);
    if (existing) {
      existing.push(meal);
    } else {
      groups.set(meal.date, [meal]);
    }
  }

  return [...groups.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, meals]) => ({ date, label: dayLabel(date), meals }));
});

function dayLabel(date: string): string {
  // Parsed as local midday, not midnight: a plain yyyy-MM-dd is parsed as UTC, which in a
  // negative offset shows the day before.
  return new Date(`${date}T12:00:00`).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

async function add() {
  isAdding.value = true;
  emit('add');
  isAdding.value = false;
}
</script>

<template>
  <div class="rounded-2xl border border-green-200 bg-green-50 p-3 dark:border-green-900 dark:bg-green-950/40">
    <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-green-700 dark:text-green-400">
      Suggested meals
    </p>

    <div class="space-y-2">
      <div v-for="day in byDate" :key="day.date">
        <p class="text-xs font-semibold text-gray-500 dark:text-gray-400">{{ day.label }}</p>
        <ul class="mt-0.5 space-y-0.5">
          <li
            v-for="(meal, index) in day.meals"
            :key="`${meal.mealType}-${index}`"
            class="text-sm text-gray-700 dark:text-gray-200"
          >
            <span class="font-medium">{{ meal.mealType }}:</span>
            {{ meal.name }}
            <span v-if="meal.description" class="text-gray-500 dark:text-gray-400">
              &mdash; {{ meal.description }}
            </span>
          </li>
        </ul>
      </div>
    </div>

    <p
      v-if="isAccepted"
      class="mt-3 text-sm font-medium text-green-700 dark:text-green-400"
    >
      Added to your meal plan.
    </p>
    <button
      v-else-if="canAdd !== false"
      type="button"
      :disabled="isAdding"
      class="mt-3 w-full rounded-xl bg-green-600 py-2 text-sm font-semibold text-white transition-colors hover:bg-green-700 disabled:opacity-60"
      @click="add"
    >
      Add {{ suggestions.length }} {{ suggestions.length === 1 ? 'meal' : 'meals' }} to my plan
    </button>
  </div>
</template>
