<script setup lang="ts">
import { computed, ref } from 'vue';
import type { MealAnalysis, MealType } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';
import AppModal from '@/components/ui/AppModal.vue';
import ScoreDonut from '@/components/ui/ScoreDonut.vue';
import AnalysisGapList from '@/components/ai/AnalysisGapList.vue';
import AnalysisSuggestionList from '@/components/ai/AnalysisSuggestionList.vue';
import { scorePillClass } from '@/utils/analysisScore';

const props = defineProps<{
  analysis: MealAnalysis;
  mealType: MealType;
}>();

/**
 * The badge owns its own dialog rather than reporting a click upward: nothing outside this
 * component needs to know the analysis was opened, and every meal row would otherwise have
 * to thread the same state back through the page.
 */
const open = ref(false);

const pillClass = computed(() => scorePillClass(props.analysis.score));

const title = computed(() => `${MEAL_TYPE_LABELS[props.mealType] ?? 'Meal'} analysis`);

const analyzedOn = computed(() =>
  props.analysis.generatedAt
    ? new Date(props.analysis.generatedAt).toLocaleString(undefined, {
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
      })
    : null,
);

</script>

<template>
  <button
    type="button"
    :class="[
      'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-semibold transition-opacity hover:opacity-80',
      pillClass,
    ]"
    :title="`AI analysis: ${analysis.score}/100. Click to read it.`"
    @click.stop="open = true"
  >
    <span aria-hidden="true">✨</span>
    {{ analysis.score }}
  </button>

  <AppModal v-model="open" :title="title" size="md">
    <div class="space-y-4">
      <div class="flex items-center gap-3">
        <ScoreDonut :score="analysis.score" size="sm" />
        <div>
          <p class="text-xs font-semibold uppercase tracking-wide text-purple-700 dark:text-purple-400">
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

      <!-- Read-only on purpose: re-running costs an AI call, and the button that spends it
           already lives in the meal dialog next to the meal it would re-read. -->
      <p class="border-t border-gray-100 pt-3 text-xs text-gray-400 dark:border-gray-800">
        Edit this meal to re-run the analysis.
      </p>
    </div>
  </AppModal>
</template>
