<script setup lang="ts">
import { computed } from 'vue';
import { caloriesOf, stepFor, type MealItem } from '@/composables/useMealItems';
import type { QuickAddSource } from '@foodeez/shared';

const props = defineProps<{ entry: MealItem }>();
const emit = defineEmits<{
  adjust: [delta: number];
  setAmount: [amount: number];
  remove: [];
}>();

const step = computed(() => stepFor(props.entry.item));
const calories = computed(() => Math.round(caloriesOf(props.entry)));

/**
 * Which numbers were looked up and which were guessed at, per line, so a quick-added meal can
 * be checked at a glance instead of taken on faith.
 *
 * Partial on purpose: a line from a saved meal is one the reader built and approved
 * themselves, and labelling it would only add noise.
 */
const SOURCE_BADGES: Partial<
  Record<QuickAddSource, { text: string; title: string; classes: string }>
> = {
  Estimated: {
    text: 'AI estimate',
    title:
      'Nothing in the food database matched this, so these numbers are an estimate. Worth a glance.',
    classes: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
  },
  Matched: {
    text: 'from database',
    title: 'Matched to a food already on file - these are its real numbers.',
    classes: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
  },
};

const badge = computed(() => (props.entry.source ? SOURCE_BADGES[props.entry.source] : null));

function onAmountChange(event: Event): void {
  emit('setAmount', parseFloat((event.target as HTMLInputElement).value));
}
</script>

<template>
  <div class="space-y-2 rounded-lg bg-gray-50 p-3 dark:bg-gray-800">
    <div class="flex items-start justify-between gap-2">
      <div>
        <div class="flex flex-wrap items-center gap-1.5">
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ entry.item.name }}</p>
          <span
            v-if="badge"
            :class="['rounded-full px-1.5 py-0.5 text-[10px] font-semibold', badge.classes]"
            :title="badge.title"
          >
            {{ badge.text }}
          </span>
        </div>
        <p class="text-xs text-gray-400">
          1 serving = {{ entry.item.servingSize }}{{ entry.item.servingUnit }}
        </p>
      </div>
      <button
        type="button"
        class="mt-0.5 shrink-0 text-red-400 hover:text-red-600 dark:hover:text-red-400"
        :aria-label="`Remove ${entry.item.name}`"
        @click="emit('remove')"
      >
        x
      </button>
    </div>

    <div class="flex items-center gap-2">
      <button
        type="button"
        class="flex h-7 w-7 items-center justify-center rounded-full bg-gray-200 text-base leading-none text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200"
        :aria-label="`Less ${entry.item.name}`"
        @click="emit('adjust', -step)"
      >
        -
      </button>
      <input
        :value="entry.amount"
        :min="step"
        :step="step"
        type="number"
        class="w-20 rounded-lg border border-gray-300 py-1 text-center text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-green-500 dark:border-gray-600"
        :aria-label="`Amount of ${entry.item.name} in ${entry.item.servingUnit}`"
        @change="onAmountChange"
      />
      <button
        type="button"
        class="flex h-7 w-7 items-center justify-center rounded-full bg-gray-200 text-base leading-none text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200"
        :aria-label="`More ${entry.item.name}`"
        @click="emit('adjust', step)"
      >
        +
      </button>
      <span class="text-sm text-gray-500 dark:text-gray-400">{{ entry.item.servingUnit }}</span>
      <span class="ml-auto text-sm tabular-nums text-gray-400">{{ calories }} kcal</span>
    </div>
  </div>
</template>
