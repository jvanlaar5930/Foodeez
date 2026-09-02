<script setup lang="ts">
import { computed } from 'vue';
import {
  addDays,
  formatRange,
  fromISODate,
  startOfWeek,
  toISODate,
  type RangeMode,
} from '@/utils/dateRange';

const props = defineProps<{
  mode: RangeMode;
  startDate: string;
  endDate: string;
}>();

const emit = defineEmits<{
  'update:mode': [mode: RangeMode];
  change: [range: { startDate: string; endDate: string }];
}>();

const label = computed(() => formatRange(props.startDate, props.endDate));

/** How far one step moves: a day, or a whole week. */
const step = computed(() => (props.mode === 'week' ? 7 : 1));

function shift(direction: number) {
  const start = addDays(fromISODate(props.startDate), direction * step.value);
  emit('change', rangeFrom(start, props.mode));
}

function setMode(mode: RangeMode) {
  if (mode === props.mode) {
    return;
  }

  emit('update:mode', mode);
  // Keep the day they were looking at: switching to a week means the week containing it.
  emit('change', rangeFrom(fromISODate(props.startDate), mode));
}

function pick(value: string) {
  if (value.length === 0) {
    return;
  }

  emit('change', rangeFrom(fromISODate(value), props.mode));
}

function rangeFrom(date: Date, mode: RangeMode): { startDate: string; endDate: string } {
  if (mode === 'day') {
    const day = toISODate(date);
    return { startDate: day, endDate: day };
  }

  const monday = startOfWeek(date);
  return { startDate: toISODate(monday), endDate: toISODate(addDays(monday, 6)) };
}
</script>

<template>
  <div class="flex flex-wrap items-center justify-between gap-3">
    <div class="flex items-center gap-2">
      <button
        type="button"
        class="rounded-lg p-2 text-gray-600 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
        aria-label="Previous"
        @click="shift(-1)"
      >
        &lt;
      </button>
      <p class="min-w-40 text-center font-semibold text-gray-900 dark:text-gray-100">
        {{ label }}
      </p>
      <button
        type="button"
        class="rounded-lg p-2 text-gray-600 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
        aria-label="Next"
        @click="shift(1)"
      >
        &gt;
      </button>
    </div>

    <div class="flex items-center gap-2">
      <input
        type="date"
        :value="startDate"
        class="rounded-lg border border-gray-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        @change="pick(($event.target as HTMLInputElement).value)"
      />

      <div class="flex rounded-xl bg-gray-100 p-0.5 dark:bg-gray-800">
        <button
          v-for="option in (['day', 'week'] as RangeMode[])"
          :key="option"
          type="button"
          :class="[
            'rounded-lg px-3 py-1.5 text-sm font-semibold capitalize transition-colors',
            option === mode
              ? 'bg-white text-green-700 shadow-sm dark:bg-gray-900 dark:text-green-400'
              : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200',
          ]"
          @click="setMode(option)"
        >
          {{ option }}
        </button>
      </div>
    </div>
  </div>
</template>
