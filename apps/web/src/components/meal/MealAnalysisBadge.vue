<script setup lang="ts">
import { computed, ref } from 'vue';
import type { MealAnalysis, MealType } from '@foodeez/shared';
import { MEAL_TYPE_LABELS } from '@foodeez/shared';
import AppModal from '@/components/ui/AppModal.vue';
import { scorePillClass, scoreStroke, scoreTextClass } from '@/utils/analysisScore';

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
const ringStroke = computed(() => scoreStroke(props.analysis.score));
const ringTextClass = computed(() => scoreTextClass(props.analysis.score));

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

// The circumference of the r=24 ring the score arc is drawn on.
const RING_LENGTH = 150.8;
const arc = computed(() => `${(props.analysis.score / 100) * RING_LENGTH} ${RING_LENGTH}`);
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
        <div class="relative h-14 w-14 shrink-0">
          <svg class="h-14 w-14 -rotate-90" viewBox="0 0 56 56">
            <circle cx="28" cy="28" r="24" fill="none" stroke="#e9d5ff" stroke-width="5" />
            <circle
              cx="28"
              cy="28"
              r="24"
              fill="none"
              :stroke="ringStroke"
              stroke-width="5"
              stroke-linecap="round"
              :stroke-dasharray="arc"
            />
          </svg>
          <span
            class="absolute inset-0 flex items-center justify-center text-sm font-bold"
            :class="ringTextClass"
          >
            {{ analysis.score }}
          </span>
        </div>
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

      <div v-if="analysis.missing.length > 0">
        <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
          Missing
        </p>
        <div class="flex flex-wrap gap-1.5">
          <span
            v-for="missingItem in analysis.missing"
            :key="missingItem"
            class="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/40 dark:text-red-300"
          >
            {{ missingItem }}
          </span>
        </div>
      </div>

      <div v-if="analysis.suggestions.length > 0">
        <p class="mb-1.5 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
          Suggestions
        </p>
        <ul class="space-y-1">
          <li
            v-for="suggestion in analysis.suggestions"
            :key="suggestion"
            class="flex items-start gap-1.5 text-sm text-gray-700 dark:text-gray-200"
          >
            <span class="mt-0.5 shrink-0 text-green-500">&gt;</span>{{ suggestion }}
          </li>
        </ul>
      </div>

      <!-- Read-only on purpose: re-running costs an AI call, and the button that spends it
           already lives in the meal dialog next to the meal it would re-read. -->
      <p class="border-t border-gray-100 pt-3 text-xs text-gray-400 dark:border-gray-800">
        Edit this meal to re-run the analysis.
      </p>
    </div>
  </AppModal>
</template>
