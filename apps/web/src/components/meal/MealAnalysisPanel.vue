<script setup lang="ts">
import LoadingSpinner from '@/components/ui/LoadingSpinner.vue';
import { computed } from 'vue';
import StreamingText from '@/components/ai/StreamingText.vue';
import AnalysisGapList from '@/components/ai/AnalysisGapList.vue';
import AnalysisSuggestionList from '@/components/ai/AnalysisSuggestionList.vue';
import ScoreDonut from '@/components/ui/ScoreDonut.vue';
import type { MealAnalysisResult } from '@/services/aiService';

const props = defineProps<{
  analysis: MealAnalysisResult | null;
  isAnalyzing: boolean;
  /** What the model has written so far, shown while it writes. */
  streamedText: string;
  error: string | null;
}>();

const emit = defineEmits<{ analyze: [] }>();

const label = computed(() => {
  if (props.isAnalyzing) return 'Analyzing...';

  return props.analysis ? 'Re-run AI Analysis' : 'AI Meal Analysis';
});

const analyzedOn = computed(() =>
  props.analysis?.generatedAt
    ? new Date(props.analysis.generatedAt).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
      })
    : null,
);
</script>

<template>
  <div>
    <button
      type="button"
      class="mt-4 flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-purple-300 py-2 text-sm font-semibold text-purple-600 transition-colors hover:bg-purple-50 disabled:opacity-50 dark:text-purple-400 dark:hover:bg-purple-950/40"
      :disabled="isAnalyzing"
      @click="emit('analyze')"
    >
      <LoadingSpinner v-if="isAnalyzing" size="sm" color="currentColor" />
      <span v-else aria-hidden="true">*</span>
      {{ label }}
    </button>

    <div
      v-if="isAnalyzing || error"
      class="mt-3 rounded-xl border border-purple-200 bg-purple-50 p-4 dark:border-purple-900 dark:bg-purple-950/40"
    >
      <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
      <StreamingText v-else :text="streamedText" placeholder="Reading your meal..." />
    </div>

    <div
      v-if="analysis"
      class="mt-3 space-y-3 rounded-xl border border-purple-200 bg-purple-50 p-4 dark:bg-purple-950/40"
    >
      <div class="flex items-center gap-3">
        <ScoreDonut :score="analysis.score" size="sm" />
        <div>
          <p
            class="text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400"
          >
            Meal Score
          </p>
          <p class="text-sm leading-snug text-gray-700 dark:text-gray-200">
            {{ analysis.completeness }}
          </p>
          <p v-if="analyzedOn" class="mt-0.5 text-xs text-gray-400">Analyzed {{ analyzedOn }}</p>
        </div>
      </div>

      <AnalysisGapList label="Missing" :items="analysis.missing" />
      <AnalysisSuggestionList label="Suggestions" :items="analysis.suggestions" />
    </div>
  </div>
</template>
