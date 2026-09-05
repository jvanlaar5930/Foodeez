<script setup lang="ts">
import { extractErrorMessage } from '@/utils/apiError';

const ANALYSIS_FAILED = 'The analysis could not be completed. Please try again.';
import { computed, ref, watch } from 'vue';
import type { DayAnalysis } from '@foodeez/shared';
import { aiService } from '@/services/aiService';
import { AIStreamError } from '@/services/aiStream';
import StreamingText from '@/components/ai/StreamingText.vue';
import AnalysisGapList from '@/components/ai/AnalysisGapList.vue';
import AnalysisSuggestionList from '@/components/ai/AnalysisSuggestionList.vue';
import ScoreDonut from '@/components/ui/ScoreDonut.vue';
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
/** What the model has written so far, shown while it writes. */
const streamedText = ref('');

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
  streamedText.value = '';

  try {
    analysis.value = await aiService.analyzeDayStream(props.userId, props.date, refresh, (text) => {
      streamedText.value += text;
    });
  } catch (err: unknown) {
    error.value = extractErrorMessage(err, ANALYSIS_FAILED);
  } finally {
    isAnalyzing.value = false;
  }
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

    <div v-if="isAnalyzing" class="mt-3">
      <StreamingText :text="streamedText" :placeholder="`Reviewing ${dayLabel}'s meals...`" />
    </div>

    <template v-else-if="analysis">
      <div class="mt-3 flex items-start gap-3">
        <ScoreDonut :score="analysis.score" size="md" />
        <div class="min-w-0">
          <p class="text-sm leading-snug text-gray-700 dark:text-gray-200">{{ analysis.status }}</p>
          <p v-if="analyzedAt" class="mt-1 text-xs text-gray-400">Analyzed at {{ analyzedAt }}</p>
        </div>
      </div>

      <AnalysisGapList label="Still short on" :items="analysis.gaps" class="mt-3" />

      <AnalysisSuggestionList
        :label="isToday ? 'What to have next' : 'What would have rounded it out'"
        :items="analysis.recommendations"
        class="mt-3"
      />
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
