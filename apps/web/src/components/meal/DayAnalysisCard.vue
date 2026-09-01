<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { DayAnalysis } from '@foodeez/shared';
import { aiService } from '@/services/aiService';
import { useMealStore } from '@/stores/meal';

const props = defineProps<{
  userId: string;
  date: string;
  isToday: boolean;
}>();

const mealStore = useMealStore();

const analysis = ref<DayAnalysis | null>(null);
const isAnalyzing = ref(false);
const error = ref<string | null>(null);

/**
 * Changes whenever the day's meals do - which is exactly when the server retires the stored
 * analysis, so it is also when this card should go back and ask what is still on file.
 */
const daySignature = computed(() =>
  mealStore.dailyLogs
    .map(
      (log) =>
        `${log.mealType}:${log.items.map((item) => `${item.foodItem.id}x${item.quantity}`).join(',')}`,
    )
    .join('|'),
);

const dayLabel = computed(() => (props.isToday ? 'today' : 'this day'));

const scoreColor = computed(() => {
  const score = analysis.value?.score ?? 0;
  if (score >= 75) {
    return '#16a34a';
  }
  if (score >= 50) {
    return '#f59e0b';
  }
  return '#ef4444';
});

const scoreTextColor = computed(() => {
  const score = analysis.value?.score ?? 0;
  if (score >= 75) {
    return 'text-green-600 dark:text-green-400';
  }
  if (score >= 50) {
    return 'text-amber-600';
  }
  return 'text-red-600 dark:text-red-400';
});

const analyzedAt = computed(() =>
  analysis.value?.generatedAt
    ? new Date(analysis.value.generatedAt).toLocaleTimeString(undefined, {
        hour: 'numeric',
        minute: '2-digit',
      })
    : null,
);

/** Only reads what is already stored - never spends an AI call. */
async function loadStored() {
  analysis.value = null;
  error.value = null;

  if (!props.userId || mealStore.dailyLogs.length === 0) {
    return;
  }

  try {
    analysis.value = await aiService.getDayAnalysis(props.userId, props.date);
  } catch {
    analysis.value = null;
  }
}

async function runAnalysis(refresh: boolean) {
  if (!props.userId) {
    return;
  }

  isAnalyzing.value = true;
  error.value = null;

  try {
    analysis.value = await aiService.analyzeDay(props.userId, props.date, refresh);
  } catch (err: unknown) {
    error.value = extractErrorMessage(err);
  } finally {
    isAnalyzing.value = false;
  }
}

function extractErrorMessage(err: unknown): string {
  if (err && typeof err === 'object' && 'response' in err) {
    const e = err as { response?: { data?: { message?: string; detail?: string; title?: string } } };
    return (
      e.response?.data?.message ??
      e.response?.data?.detail ??
      e.response?.data?.title ??
      'The analysis could not be completed. Please try again.'
    );
  }

  return 'The analysis could not be completed. Please try again.';
}

watch([() => props.date, () => props.userId, daySignature], loadStored, { immediate: true });
</script>

<template>
  <div
    class="rounded-2xl border border-purple-200 dark:border-purple-900 bg-purple-50 dark:bg-purple-950/40 p-4"
  >
    <div class="flex items-center justify-between gap-3">
      <p class="text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">
        AI Day Analysis
      </p>
      <button
        v-if="analysis && !isAnalyzing"
        class="text-xs font-semibold text-purple-700 dark:text-purple-400 hover:underline"
        @click="runAnalysis(true)"
      >
        Re-run
      </button>
    </div>

    <div v-if="isAnalyzing" class="mt-3 flex items-center gap-2 text-sm text-purple-700 dark:text-purple-300">
      <span class="h-4 w-4 animate-spin rounded-full border-2 border-purple-400 border-t-transparent" />
      Reviewing {{ dayLabel }}'s meals...
    </div>

    <template v-else-if="analysis">
      <div class="mt-3 flex items-start gap-3">
        <div class="relative h-16 w-16 shrink-0">
          <svg class="h-16 w-16 -rotate-90" viewBox="0 0 64 64">
            <circle cx="32" cy="32" r="28" fill="none" stroke="#e9d5ff" stroke-width="5" />
            <circle
              cx="32"
              cy="32"
              r="28"
              fill="none"
              :stroke="scoreColor"
              stroke-width="5"
              stroke-linecap="round"
              :stroke-dasharray="`${(analysis.score / 100) * 175.9} 175.9`"
            />
          </svg>
          <span
            class="absolute inset-0 flex items-center justify-center text-base font-bold"
            :class="scoreTextColor"
          >
            {{ analysis.score }}
          </span>
        </div>
        <div class="min-w-0">
          <p class="text-sm leading-snug text-gray-700 dark:text-gray-200">{{ analysis.status }}</p>
          <p v-if="analyzedAt" class="mt-1 text-xs text-gray-400">Analyzed at {{ analyzedAt }}</p>
        </div>
      </div>

      <div v-if="analysis.gaps.length > 0" class="mt-3">
        <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
          Still short on
        </p>
        <div class="flex flex-wrap gap-1.5">
          <span
            v-for="gap in analysis.gaps"
            :key="gap"
            class="rounded-full bg-red-100 dark:bg-red-900/40 px-2 py-0.5 text-xs font-medium text-red-700 dark:text-red-300"
          >
            {{ gap }}
          </span>
        </div>
      </div>

      <div v-if="analysis.recommendations.length > 0" class="mt-3">
        <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
          {{ isToday ? 'What to have next' : 'What would have rounded it out' }}
        </p>
        <ul class="space-y-1">
          <li
            v-for="recommendation in analysis.recommendations"
            :key="recommendation"
            class="flex items-start gap-1.5 text-sm text-gray-700 dark:text-gray-200"
          >
            <span class="mt-0.5 shrink-0 text-green-500">&gt;</span>{{ recommendation }}
          </li>
        </ul>
      </div>
    </template>

    <template v-else>
      <p class="mt-2 text-sm text-gray-600 dark:text-gray-300">
        See how {{ dayLabel }}'s meals stack up against your targets, and what to eat to finish the
        day balanced.
      </p>
      <button
        class="mt-3 flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-purple-300 dark:border-purple-800 py-2 text-sm font-semibold text-purple-700 dark:text-purple-400 transition-colors hover:bg-purple-100 dark:hover:bg-purple-900/40"
        @click="runAnalysis(false)"
      >
        Analyze {{ dayLabel }}'s meals
      </button>
    </template>

    <p v-if="error" class="mt-3 text-sm text-red-600 dark:text-red-400">{{ error }}</p>
  </div>
</template>
